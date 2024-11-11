﻿using NINA.Plugin.TargetScheduler.Database.Schema;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;

namespace NINA.Plugin.TargetScheduler.Planning.Interfaces {

    public interface IScoringEngine {
        IProfile ActiveProfile { get; }
        ProfilePreference ProfilePreference { get; set; }
        DateTime AtTime { get; }
        ITarget PreviousPlanTarget { get; }
        Dictionary<string, double> RuleWeights { get; set; }
        List<IScoringRule> Rules { get; set; }

        double ScoreTarget(ITarget potentialTarget);
    }
}