﻿using NINA.Astrometry;
using NINA.Plugin.TargetScheduler.Astrometry;
using NINA.Plugin.TargetScheduler.Database.Schema;
using NINA.Plugin.TargetScheduler.Planning.Interfaces;
using NINA.Plugin.TargetScheduler.Shared.Utility;
using NINA.Plugin.TargetScheduler.Util;
using NINA.Profile.Interfaces;
using System;

namespace NINA.Plugin.TargetScheduler.Planning {

    /// <summary>
    /// Core methods to determine the suitability of a target and target exposures for imaging.  Note that
    /// these methods cause side-effects on the provided target and exposures, for example setting
    /// rejection details.
    /// </summary>
    public class TargetImagingExpert {
        private const int TARGET_VISIBILITY_SAMPLE_INTERVAL = 10;
        private const int TARGET_FUTURE_TEST_SAMPLE_INTERVAL = 60;

        private IProfile activeProfile;
        private ProfilePreference profilePreferences;
        private ObserverInfo observerInfo;

        public TargetImagingExpert(IProfile activeProfile, ProfilePreference profilePreferences) {
            this.activeProfile = activeProfile;
            this.profilePreferences = profilePreferences;
            this.observerInfo = new ObserverInfo {
                Latitude = activeProfile.AstrometrySettings.Latitude,
                Longitude = activeProfile.AstrometrySettings.Longitude,
                Elevation = activeProfile.AstrometrySettings.Elevation,
            };
        }

        public bool Visibility(DateTime atTime, ITarget target) {
            TwilightCircumstances twilightCircumstances = TwilightCircumstances.AdjustTwilightCircumstances(observerInfo, atTime);
            TargetVisibility targetVisibility = new(target, observerInfo,
                twilightCircumstances.OnDate, twilightCircumstances.Sunset, twilightCircumstances.Sunrise, TARGET_VISIBILITY_SAMPLE_INTERVAL);
            return Visibility(atTime, target, twilightCircumstances, targetVisibility);
        }

        /// <summary>
        /// Determine visibility for the target at the provided time.
        /// </summary>
        /// <param name="atTime"></param>
        /// <param name="target"></param>
        /// <param name="twilightCircumstances"></param>
        /// <param name="targetVisibility"></param>
        /// <returns></returns>
        public bool Visibility(DateTime atTime, ITarget target, TwilightCircumstances twilightCircumstances, TargetVisibility targetVisibility) {
            if (target.Rejected) { return false; }
            IProject project = target.Project;

            if (!AstrometryUtils.RisesAtLocation(observerInfo, target.Coordinates)) {
                TSLogger.Warning($"target {project.Name}/{target.Name} never rises at location - skipping");
                SetRejected(target, Reasons.TargetNeverRises);
                return false;
            }

            // Get the most inclusive twilight over all incomplete exposure plans
            TimeInterval twilightSpan = twilightCircumstances.GetTwilightSpan(GetOverallTwilight(target));

            // At high latitudes near the summer solsice, you can lose nighttime completely (even below the polar circle)
            if (twilightSpan == null) {
                TSLogger.Warning($"No twilight span for target {project.Name}/{target.Name} on {Utils.FormatDateTimeFull(atTime)} at latitude {observerInfo.Latitude}");
                SetRejected(target, Reasons.TargetAllExposurePlans);
                return false;
            }

            if (!targetVisibility.ImagingPossible) {
                TSLogger.Debug($"Target not visible at all {project.Name}/{target.Name} on {Utils.FormatDateTimeFull(atTime)} at latitude {observerInfo.Latitude}");
                SetRejected(target, Reasons.TargetNotVisible);
                return false;
            }

            // Determine the next time interval of visibility of at least the mimimum time
            VisibilityDetermination viz = targetVisibility.NextVisibleInterval(atTime, twilightSpan, project.HorizonDefinition, project.MinimumTime * 60);
            if (!viz.IsVisible) {
                TSLogger.Debug($"Target not visible for rest of night {project.Name}/{target.Name} on {Utils.FormatDateTimeFull(atTime)} at latitude {observerInfo.Latitude}");
                SetRejected(target, Reasons.TargetNotVisible);
                return false;
            }

            DateTime targetStartTime = viz.StartTime;
            DateTime targetTransitTime = targetVisibility.TransitTime;
            DateTime targetEndTime = viz.StopTime;

            // Clip time span to optional meridian window
            TimeInterval meridianClippedSpan = null;
            if (project.MeridianWindow > 0) {
                TSLogger.Debug($"checking meridian window for {project.Name}/{target.Name}");
                meridianClippedSpan = new MeridianWindowClipper().Clip(targetStartTime, targetTransitTime, targetEndTime, project.MeridianWindow);

                if (meridianClippedSpan == null) {
                    SetRejected(target, Reasons.TargetMeridianWindowClipped);
                    return false;
                }

                target.MeridianWindow = meridianClippedSpan;
                targetStartTime = meridianClippedSpan.StartTime;
                targetEndTime = meridianClippedSpan.EndTime;
            }

            // If the start time is in the future, reject ... for now
            DateTime actualStart = atTime > targetStartTime ? atTime : targetStartTime;
            if (actualStart > atTime) {
                target.StartTime = actualStart;
                target.EndTime = targetEndTime;
                string reason = meridianClippedSpan != null ? Reasons.TargetBeforeMeridianWindow : Reasons.TargetNotYetVisible;
                SetRejected(target, reason);
                return false;
            }

            // Otherwise the target is a candidate
            target.StartTime = targetStartTime;
            target.EndTime = targetEndTime;
            target.CulminationTime = targetTransitTime;
            return true;
        }

        /// <summary>
        /// Reject target exposures that are not suitable for the current level of twilight.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="currentTwilightLevel"></param>
        public void TwilightFilter(ITarget target, TwilightLevel? currentTwilightLevel) {
            if (target.Rejected) { return; }

            foreach (IExposure exposure in target.ExposurePlans) {
                if (!exposure.Rejected && exposure.IsIncomplete()) {
                    if (currentTwilightLevel.HasValue) {
                        if (currentTwilightLevel > exposure.TwilightLevel)
                            SetRejected(exposure, Reasons.FilterTwilight);
                    } else {
                        SetRejected(exposure, Reasons.FilterTwilight);
                    }
                }
            }
        }

        /// <summary>
        /// Reject target exposures that are not suitable based on moon avoidance settings.
        /// </summary>
        /// <param name="atTime"></param>
        /// <param name="target"></param>
        /// <param name="moonExpert"></param>
        public void MoonAvoidanceFilter(DateTime atTime, ITarget target, IMoonAvoidanceExpert moonExpert) {
            if (target.Rejected && target.RejectedReason != Reasons.TargetNotYetVisible) { return; }

            foreach (IExposure exposure in target.ExposurePlans) {
                if (exposure.IsIncomplete()) {
                    if (moonExpert.IsRejected(atTime, target, exposure)) {
                        SetRejected(exposure, Reasons.FilterMoonAvoidance);
                    }
                }
            }
        }

        /// <summary>
        /// Determine if the target is ready to image at the provided time, taking the visibility sampling
        /// interval into account.
        /// </summary>
        /// <param name="atTime"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public bool ReadyNow(DateTime atTime, ITarget target) {
            if (target.Rejected) { return false; }

            TimeSpan diff = atTime - target.StartTime;
            return Math.Abs(diff.TotalSeconds) <= TARGET_VISIBILITY_SAMPLE_INTERVAL * 2;
        }

        /// <summary>
        /// Find the next possible time that a target could be imaged, taking into account target
        /// visibility and the suitability of exposures given the state of circumstances (like
        /// moon avoidance) at that time.
        ///
        /// If future imaging is possible, the target's start time will be advanced to that time.
        /// Otherwise, the target will be marked rejected.
        /// </summary>
        /// <param name="target"></param>
        public void CheckFuture(ITarget target, IMoonAvoidanceExpert moonExpert) {
            DateTime atTime = target.StartTime;
            TwilightCircumstances twilightCircumstances = TwilightCircumstances.AdjustTwilightCircumstances(observerInfo, atTime);
            TargetVisibility targetVisibility = new(target, observerInfo,
                twilightCircumstances.OnDate, twilightCircumstances.Sunset, twilightCircumstances.Sunrise,
                TARGET_VISIBILITY_SAMPLE_INTERVAL);

            while (true) {
                // Check target for moon avoidance and twilight at this time
                MoonAvoidanceFilter(atTime, target, moonExpert);
                if (AllExposurePlansRejected(target)) {
                    SetRejected(target, Reasons.TargetMoonAvoidance);
                }

                if (!target.Rejected) {
                    TwilightFilter(target, twilightCircumstances.GetCurrentTwilightLevel(atTime));
                    if (AllExposurePlansRejected(target)) {
                        SetRejected(target, Reasons.FilterTwilight);
                    }
                }

                // If not rejected, we've found a future time at which the target could be imaged
                if (!target.Rejected) {
                    target.StartTime = atTime;
                    return;
                }

                // Otherwise, advance time and check target visibility at the new time
                atTime = atTime.AddSeconds(TARGET_FUTURE_TEST_SAMPLE_INTERVAL);
                ClearRejections(target);
                if (Visibility(atTime, target, twilightCircumstances, targetVisibility)) {
                    atTime = target.StartTime;
                } else if (VisibleLater(target)) {
                    atTime = target.StartTime;
                } else {
                    return; // no more visibility this night
                }
            }
        }

        public bool VisibleLater(ITarget target) {
            return target.Rejected &&
                (target.RejectedReason == Reasons.TargetNotYetVisible || target.RejectedReason == Reasons.TargetBeforeMeridianWindow);
        }

        public bool AllExposurePlansRejected(ITarget target) {
            foreach (IExposure exposure in target.ExposurePlans) {
                if (!exposure.Rejected) {
                    return false;
                }
            }

            return true;
        }

        public void ClearRejections(ITarget target) {
            target.Rejected = false;
            target.RejectedReason = null;
            target.ExposurePlans.ForEach(e => { e.Rejected = false; e.RejectedReason = null; });
        }

        private TwilightLevel GetOverallTwilight(ITarget target) {
            TwilightLevel twilightLevel = TwilightLevel.Nighttime;
            foreach (IExposure exposure in target.ExposurePlans) {
                // find most permissive (brightest) twilight over all incomplete plans
                if (exposure.TwilightLevel > twilightLevel && exposure.IsIncomplete()) {
                    twilightLevel = exposure.TwilightLevel;
                }
            }

            return twilightLevel;
        }

        private void SetRejected(ITarget target, string reason) {
            target.Rejected = true;
            target.RejectedReason = reason;
        }

        private void SetRejected(IExposure exposure, string reason) {
            exposure.Rejected = true;
            exposure.RejectedReason = reason;
        }
    }
}