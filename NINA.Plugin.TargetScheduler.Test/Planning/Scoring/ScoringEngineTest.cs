﻿using FluentAssertions;
using Moq;
using NINA.Core.Enum;
using NINA.Core.Utility;
using NINA.Plugin.TargetScheduler.Database.Schema;
using NINA.Plugin.TargetScheduler.Planning.Interfaces;
using NINA.Plugin.TargetScheduler.Planning.Scoring;
using NINA.Plugin.TargetScheduler.Planning.Scoring.Rules;
using NINA.Plugin.TargetScheduler.Test.Astrometry;
using NINA.Profile.Interfaces;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace NINA.Plugin.TargetScheduler.Test.Planning.Scoring {

    [TestFixture]
    public class ScoringEngineTest {

        [Test]
        public void TestOneRule() {
            Mock<IProfile> profileMock = new Mock<IProfile>();
            ProfilePreference profilePreference = new ProfilePreference();

            Dictionary<string, double> ruleWeights = new Dictionary<string, double>();
            ruleWeights.Add("TestRule1", 75);
            List<IScoringRule> rules = new List<IScoringRule>();
            rules.Add(new TestRule("TestRule1", 0.5));

            ScoringEngine sut = new ScoringEngine(profileMock.Object, profilePreference, DateTime.Now, null);
            sut.RuleWeights = ruleWeights;
            sut.Rules = rules;

            Mock<ITarget> pt = PlanMocks.GetMockPlanTarget("M42", TestData.M42);
            sut.ScoreTarget(pt.Object).Should().BeApproximately(0.375, 0.00001);
        }

        [Test]
        public void TestTwoRules() {
            Logger.SetLogLevel(LogLevelEnum.DEBUG);
            Mock<IProfile> profileMock = new Mock<IProfile>();
            ProfilePreference profilePreference = new ProfilePreference();

            Dictionary<string, double> ruleWeights = new Dictionary<string, double>();
            ruleWeights.Add("TestRule1", 100);
            ruleWeights.Add("TestRule2", 50);
            List<IScoringRule> rules = new List<IScoringRule>();
            rules.Add(new TestRule("TestRule1", 1));
            rules.Add(new TestRule("TestRule2", 0.5));

            ScoringEngine sut = new ScoringEngine(profileMock.Object, profilePreference, DateTime.Now, null);
            sut.RuleWeights = ruleWeights;
            sut.Rules = rules;

            Mock<ITarget> pt = PlanMocks.GetMockPlanTarget("M42", TestData.M42);
            sut.ScoreTarget(pt.Object).Should().BeApproximately(1.25, 0.00001);
        }
    }

    internal class TestRule : ScoringRule {
        private string name;
        private double score;

        public TestRule(string name, double score) {
            this.name = name;
            this.score = score;
        }

        public override string Name { get { return name; } }
        public override double DefaultWeight { get { return 1; } }

        public override double Score(IScoringEngine scoringEngine, ITarget potentialTarget) {
            return score;
        }
    }
}