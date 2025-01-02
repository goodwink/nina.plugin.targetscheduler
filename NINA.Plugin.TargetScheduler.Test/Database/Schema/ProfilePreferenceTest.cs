﻿using FluentAssertions;
using NINA.Plugin.TargetScheduler.Database.Schema;
using NUnit.Framework;

namespace NINA.Plugin.TargetScheduler.Test.Database.Schema {

    [TestFixture]
    public class ProfilePreferenceTest {

        [Test]
        public void TestProfilePreference() {
            ProfilePreference sut = new ProfilePreference("pid");
            sut.ProfileId.Should().Be("pid");
            sut.ParkOnWait.Should().BeFalse();
            sut.ExposureThrottle.Should().BeApproximately(125, 0.001);
            sut.EnableSmartPlanWindow.Should().BeTrue();

            sut.EnableGradeRMS.Should().BeTrue();
            sut.EnableGradeStars.Should().BeTrue();
            sut.EnableGradeHFR.Should().BeTrue();
            sut.EnableGradeFWHM.Should().BeFalse();
            sut.EnableGradeEccentricity.Should().BeFalse();
            sut.DelayGrading.Should().BeApproximately(0, 0.001);
            sut.AcceptImprovement.Should().BeTrue();
            sut.MaxGradingSampleSize.Should().Be(10);
            sut.RMSPixelThreshold.Should().Be(8);
            sut.DetectedStarsSigmaFactor.Should().BeApproximately(4, 0.001);
            sut.HFRSigmaFactor.Should().BeApproximately(4, 0.001);
            sut.FWHMSigmaFactor.Should().BeApproximately(4, 0.001);
            sut.EccentricitySigmaFactor.Should().BeApproximately(4, 0.001);

            sut.EnableSynchronization.Should().BeFalse();
            sut.SyncWaitTimeout.Should().Be(300);
            sut.SyncActionTimeout.Should().Be(300);
            sut.SyncSolveRotateTimeout.Should().Be(300);
        }
    }
}