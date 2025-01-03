﻿using NINA.Core.Enum;
using NINA.Core.Model;
using NINA.Plugin.TargetScheduler.Planning;
using NINA.Plugin.TargetScheduler.Planning.Entities;
using NINA.Plugin.TargetScheduler.Planning.Interfaces;
using NINA.Plugin.TargetScheduler.Shared.Utility;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.Container.ExecutionStrategy;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Plugin.TargetScheduler.Sequencer {

    /// <summary>
    /// Modified from NINA.Sequencer.Container.ExecutionStrategy.SequentialStrategy.  This strategy manages the
    /// status monitor to keep track of progress for the parent TargetSchedulerContainer.
    /// </summary>
    public class PlanTargetContainerStrategy : IExecutionStrategy {

        public object Clone() {
            return new PlanTargetContainerStrategy();
        }

        private ISequenceContainer parentContainer;
        private SchedulerProgressVM schedulerProgress;
        private Queue<InstructionMonitor> instructionProgressQueue;

        public void SetContext(TargetSchedulerContainer parentContainer, SchedulerPlan schedulerPlan, SchedulerProgressVM schedulerProgress) {
            this.parentContainer = parentContainer;
            this.schedulerProgress = schedulerProgress;

            instructionProgressQueue = new Queue<InstructionMonitor>();
            foreach (IInstruction instruction in schedulerPlan.PlanInstructions) {
                if (instruction is PlanMessage) {
                    continue;
                }

                if (instruction is PlanSlew) {
                    instructionProgressQueue.Enqueue(new InstructionMonitor("Slew"));
                    continue;
                }

                if (instruction is PlanSwitchFilter) {
                    instructionProgressQueue.Enqueue(new InstructionMonitor("SwitchFilter", instruction.exposure.FilterName));
                    continue;
                }

                if (instruction is PlanSetReadoutMode) {
                    instructionProgressQueue.Enqueue(new InstructionMonitor("SetReadoutMode"));
                    continue;
                }

                if (instruction is PlanTakeExposure) {
                    instructionProgressQueue.Enqueue(new InstructionMonitor("TakeExposure", instruction.exposure.FilterName));
                    continue;
                }

                if (instruction is PlanDither) {
                    instructionProgressQueue.Enqueue(new InstructionMonitor("Dither"));
                    continue;
                }

                if (instruction is PlanWait) {
                    instructionProgressQueue.Enqueue(new InstructionMonitor("Wait"));
                    continue;
                }

                if (instruction is PlanBeforeNewTargetContainer) {
                    instructionProgressQueue.Enqueue(new InstructionMonitor("BeforeTarget"));
                    continue;
                }

                TSLogger.Error($"Unknown instruction type in PlanTargetContainerStrategy: {instruction.GetType()}");
                throw new Exception($"Unknown instruction type in PlanTargetContainerStrategy: {instruction.GetType()}");
            }
        }

        public async Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token) {
            TSLogger.Debug("PlanTargetContainerStrategy: Execute");

            ISequenceItem previous = null;
            ISequenceItem next = null;
            bool canContinue = true;
            var root = ItemUtility.GetRootContainer(context);

            context.Iterations = 0;
            InitializeBlock(context);

            try {
                while (((next, canContinue) = GetNextItem(context, previous)).next != null && canContinue) {
                    StartBlock(context);

                    (next, canContinue) = GetNextItem(context, previous);
                    while (next != null && canContinue) {
                        token.ThrowIfCancellationRequested();
                        await RunTriggers(context, previous, next, progress, token);

                        InstructionMonitor instructionProgressItem = instructionProgressQueue.Dequeue();
                        schedulerProgress.Add(instructionProgressItem.Name, instructionProgressItem.Filter);
                        await next.Run(progress, token);

                        previous = next;

                        (next, canContinue) = GetNextItem(context, previous);
                        await RunTriggersAfter(context, previous, next, progress, token);
                    }

                    FinishBlock(context);

                    if (CanContinue(context, previous, next)) {
                        foreach (var item in context.GetItemsSnapshot()) {
                            if (item is ISequenceContainer) {
                                (item as ISequenceContainer).ResetAll();
                            } else {
                                item.ResetProgress();
                            }
                        }
                    }
                }

                //Mark rest of items as skipped
                foreach (var item in context.GetItemsSnapshot().Where(x => x.Status == SequenceEntityStatus.CREATED)) {
                    item.Skip();
                }
            } finally {
                TeardownBlock(context);
            }
        }

        private void TeardownBlock(ISequenceContainer context) {
            foreach (var item in context.GetItemsSnapshot()) {
                item.SequenceBlockTeardown();
            }

            var conditionable = context as IConditionable;
            if (conditionable != null) {
                foreach (var condition in conditionable.GetConditionsSnapshot()) {
                    condition.SequenceBlockTeardown();
                }
            }
            var triggerable = context as ITriggerable;
            if (triggerable != null) {
                foreach (var trigger in triggerable.GetTriggersSnapshot()) {
                    trigger.SequenceBlockTeardown();
                }
            }
        }

        private void InitializeBlock(ISequenceContainer context) {
            foreach (var item in context.GetItemsSnapshot()) {
                item.SequenceBlockInitialize();
            }

            var conditionable = context as IConditionable;
            if (conditionable != null) {
                foreach (var condition in conditionable.GetConditionsSnapshot()) {
                    condition.SequenceBlockInitialize();
                }
            }
            var triggerable = context as ITriggerable;
            if (triggerable != null) {
                foreach (var trigger in triggerable.GetTriggersSnapshot()) {
                    trigger.SequenceBlockInitialize();
                }
            }
        }

        private (ISequenceItem, bool) GetNextItem(ISequenceContainer context, ISequenceItem previous) {
            var items = context.GetItemsSnapshot();
            var next = items.FirstOrDefault(x => x.Status == SequenceEntityStatus.CREATED);

            var canContinue = false;
            if (next != null) {
                canContinue = CanContinue(context, previous, next);
            }

            return (next, canContinue);
        }

        private async Task RunTriggers(ISequenceContainer container, ISequenceItem previousItem, ISequenceItem nextItem, IProgress<ApplicationStatus> progress, CancellationToken token) {
            var triggerable = container as ITriggerable;
            if (triggerable != null) {
                await triggerable.RunTriggers(previousItem, nextItem, progress, token);
            }

            if (container?.Parent != null) {
                await RunTriggers(container.Parent, previousItem, nextItem, progress, token);
            }
        }

        private async Task RunTriggersAfter(ISequenceContainer container, ISequenceItem previousItem, ISequenceItem nextItem, IProgress<ApplicationStatus> progress, CancellationToken token) {
            var triggerable = container as ITriggerable;
            if (triggerable != null) {
                await triggerable.RunTriggersAfter(previousItem, nextItem, progress, token);
            }

            if (container?.Parent != null) {
                await RunTriggersAfter(container.Parent, previousItem, nextItem, progress, token);
            }
        }

        private void StartBlock(ISequenceContainer container) {
            foreach (var item in container.GetItemsSnapshot()) {
                item.SequenceBlockStarted();
            }

            var conditionable = container as IConditionable;
            if (conditionable != null) {
                foreach (var condition in conditionable.GetConditionsSnapshot()) {
                    condition.SequenceBlockStarted();
                }
            }
            var triggerable = container as ITriggerable;
            if (triggerable != null) {
                foreach (var trigger in triggerable.GetTriggersSnapshot()) {
                    trigger.SequenceBlockStarted();
                }
            }
        }

        private void FinishBlock(ISequenceContainer container) {
            container.Iterations++;

            foreach (var item in container.GetItemsSnapshot()) {
                item.SequenceBlockFinished();
            }

            var conditionable = container as IConditionable;
            if (conditionable != null) {
                foreach (var condition in conditionable.GetConditionsSnapshot()) {
                    condition.SequenceBlockFinished();
                }
            }

            var triggerable = container as ITriggerable;
            if (triggerable != null) {
                foreach (var trigger in triggerable.GetTriggersSnapshot()) {
                    trigger.SequenceBlockFinished();
                }
            }
        }

        private bool CanContinue(ISequenceContainer container, ISequenceItem previousItem, ISequenceItem nextItem) {
            // There can't be any conditions directly on PlanTargetContainer so no need to check

            // Check conditions on ancestor containers above our parent
            var canContinue = container.Iterations < 1;
            if (parentContainer.Parent != null) {
                canContinue = canContinue && AncestorsCanContinue(parentContainer.Parent, previousItem, nextItem);
            }

            return canContinue;
        }

        private bool AncestorsCanContinue(ISequenceContainer container, ISequenceItem previousItem, ISequenceItem nextItem) {
            var conditionable = container as IConditionable;
            var canContinue = false;
            var conditions = conditionable?.GetConditionsSnapshot()?.Where(x => x.Status != SequenceEntityStatus.DISABLED).ToList();

            if (conditions != null && conditions.Count > 0) {
                canContinue = conditionable.CheckConditions(previousItem, nextItem);
            } else {
                canContinue = container.Iterations < 1;
            }

            if (container.Parent != null) {
                canContinue = canContinue && AncestorsCanContinue(container.Parent, previousItem, nextItem);
            }

            return canContinue;
        }
    }

    internal class InstructionMonitor {
        public string Name { get; private set; }
        public string Filter { get; private set; }

        public InstructionMonitor(string name, string filter = "") {
            Name = name;
            Filter = filter;
        }
    }
}