﻿using NINA.Plugin.TargetScheduler.Planning.Interfaces;

namespace NINA.Plugin.TargetScheduler.Planning.Scoring.Rules {

    public class TargetSwitchPenaltyRule : ScoringRule {
        public const string RULE_NAME = "Target Switch Penalty";
        public const double DEFAULT_WEIGHT = .67 * WEIGHT_SCALE;

        public override string Name { get { return RULE_NAME; } }
        public override double DefaultWeight { get { return DEFAULT_WEIGHT; } }

        /// <summary>
        /// Score the potential target higher if it's the same as the previous target.  This assigns cost to the
        /// time to switch to another target (slew/center) and ideally prevents target thrashing.
        /// </summary>
        /// <param name="scoringEngine"></param>
        /// <param name="potentialTarget"></param>
        /// <returns></returns>
        public override double Score(IScoringEngine scoringEngine, ITarget potentialTarget) {
            return potentialTarget.Equals(scoringEngine.PreviousPlanTarget) ? 1 : 0;
        }
    }
}