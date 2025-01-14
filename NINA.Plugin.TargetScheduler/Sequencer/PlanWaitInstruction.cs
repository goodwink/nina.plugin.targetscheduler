﻿using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Plugin.TargetScheduler.Shared.Utility;
using NINA.Plugin.TargetScheduler.Util;
using NINA.Sequencer.SequenceItem;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Plugin.TargetScheduler.Sequencer {

    public class PlanWaitInstruction : SequenceItem {
        private DateTime waitUntil;
        private IGuiderMediator guiderMediator;
        private ITelescopeMediator telescopeMediator;

        public PlanWaitInstruction(IGuiderMediator guiderMediator, ITelescopeMediator telescopeMediator, DateTime waitUntil) {
            this.waitUntil = waitUntil;
            this.guiderMediator = guiderMediator;
            this.telescopeMediator = telescopeMediator;
        }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            TSLogger.Info($"stopping guiding/tracking, then waiting for {Utils.FormatDateTimeFull(waitUntil)}");
            SequenceCommands.StopGuiding(guiderMediator, token);
            SequenceCommands.SetTelescopeTracking(telescopeMediator, TrackingMode.Stopped, token);

            TimeSpan duration = waitUntil - DateTime.Now;
            CoreUtil.Wait(duration, token, progress).Wait(token);
            TSLogger.Debug("done waiting");

            return Task.CompletedTask;
        }

        public override object Clone() {
            throw new NotImplementedException();
        }
    }
}