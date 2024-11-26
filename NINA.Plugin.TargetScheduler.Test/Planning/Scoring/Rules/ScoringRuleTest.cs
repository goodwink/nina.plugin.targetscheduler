﻿using FluentAssertions;
using NINA.Plugin.TargetScheduler.Planning.Interfaces;
using NINA.Plugin.TargetScheduler.Planning.Scoring.Rules;
using NUnit.Framework;
using System.Collections.Generic;

namespace NINA.Plugin.TargetScheduler.Test.Planning.Scoring.Rules {

    [TestFixture]
    public class ScoringRuleTest {

        [Test]
        public void testGetAllScoringRules() {
            Dictionary<string, IScoringRule> map = ScoringRule.GetAllScoringRules();
            map.Should().NotBeEmpty();

            map.Should().ContainKey(ProjectPriorityRule.RULE_NAME);
            map[ProjectPriorityRule.RULE_NAME].Name.Should().Be(ProjectPriorityRule.RULE_NAME);

            map.Should().ContainKey(TargetSwitchPenaltyRule.RULE_NAME);
            map[TargetSwitchPenaltyRule.RULE_NAME].Name.Should().Be(TargetSwitchPenaltyRule.RULE_NAME);

            map.Should().ContainKey(PercentCompleteRule.RULE_NAME);
            map[PercentCompleteRule.RULE_NAME].Name.Should().Be(PercentCompleteRule.RULE_NAME);

            map.Should().ContainKey(SettingSoonestRule.RULE_NAME);
            map[SettingSoonestRule.RULE_NAME].Name.Should().Be(SettingSoonestRule.RULE_NAME);
        }
    }
}