﻿using Namotion.Reflection;
using NINA.Image.Interfaces;
using NINA.Plugin.TargetScheduler.Database.Schema;
using NINA.Plugin.TargetScheduler.Shared.Utility;
using NINA.Plugin.TargetScheduler.SyncService.Sync;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NINA.Plugin.TargetScheduler.Grading {

    public class GraderExpert {
        private IProfile profile;
        private IImageGraderPreferences preferences;
        private ImageSavedEventArgs imageData;

        public bool NoGradingMetricsEnabled => noGradingMetricsEnabled();
        public bool EnableGradeRMS => enableGradeRMS();

        public GraderExpert(GradingWorkData workData) : this(workData.GraderPreferences, workData.ImageSavedEventArgs) {
        }

        public GraderExpert(IImageGraderPreferences preferences, ImageSavedEventArgs imageData) {
            this.profile = preferences.Profile;
            this.preferences = preferences;
            this.imageData = imageData;
        }

        public bool GradeRMS() {
            if (!preferences.EnableGradeRMS) return true;

            if (imageData.MetaData?.Image?.RecordedRMS == null) {
                TSLogger.Info("image grading: guiding RMS not available");
                return true;
            }

            double guidingRMSArcSecs = imageData.MetaData.Image.RecordedRMS.Total * imageData.MetaData.Image.RecordedRMS.Scale;
            if (guidingRMSArcSecs <= 0) {
                TSLogger.Info("image grading: guiding RMS not valid for grading");
                return true;
            }

            try {
                double pixelSize = profile.CameraSettings.PixelSize;
                double focalLenth = profile.TelescopeSettings.FocalLength;
                double binning = GetBinning();
                double cameraArcSecsPerPixel = (pixelSize / focalLenth) * 206.265 * binning;
                double cameraRMSPerPixel = guidingRMSArcSecs * cameraArcSecsPerPixel;

                TSLogger.Info($"image grading: RMS pixelSize={pixelSize} focalLength={focalLenth} bin={binning} cameraArcSecsPerPixel={cameraArcSecsPerPixel} cameraRMSPerPixel={cameraRMSPerPixel}");
                return (cameraRMSPerPixel > preferences.RMSPixelThreshold) ? false : true;
            } catch (Exception e) {
                TSLogger.Warning($"image grading: failed to determine RMS error in main camera pixels: {e.Message}\n{e.StackTrace}");
                return true;
            }
        }

        public bool GradeStars(List<AcquiredImage> population) {
            if (!preferences.EnableGradeStars) return true;

            List<double> samples = GetSamples(population, i => { return i.Metadata.DetectedStars; });
            TSLogger.Info("image grading: detected star count ->");
            int detectedStars = imageData.StarDetectionAnalysis.DetectedStars;
            if (detectedStars == 0 || !WithinAcceptableVariance(samples, detectedStars, preferences.DetectedStarsSigmaFactor, true)) {
                return false;
            }

            return true;
        }

        public bool GradeHFR(List<AcquiredImage> population) {
            if (!preferences.EnableGradeHFR) return true;

            double hfr = imageData.StarDetectionAnalysis.HFR;
            if (preferences.AutoAcceptLevelHFR > 0 && hfr <= preferences.AutoAcceptLevelHFR) {
                TSLogger.Info($"image grading: HFR auto accepted: actual ({hfr}) <= level ({preferences.AutoAcceptLevelHFR})");
                return true;
            }

            List<double> samples = GetSamples(population, i => { return i.Metadata.HFR; });
            TSLogger.Info("image grading: HFR ->");
            if (NearZero(hfr) || !WithinAcceptableVariance(samples, hfr, preferences.HFRSigmaFactor, false)) {
                return false;
            }

            return true;
        }

        public bool GradeFWHM(List<AcquiredImage> population) {
            if (!preferences.EnableGradeFWHM) return true;

            double fwhm = GetHocusFocusMetric(imageData.StarDetectionAnalysis, "FWHM");
            if (Double.IsNaN(fwhm)) {
                TSLogger.Warning("image grading: FWHM grading is enabled but image doesn't have FWHM metric.  Is Hocus Focus installed, enabled, and configured for star detection?");
            } else {
                if (preferences.AutoAcceptLevelFWHM > 0 && fwhm <= preferences.AutoAcceptLevelFWHM) {
                    TSLogger.Info($"image grading: FWHM auto accepted: actual ({fwhm}) <= level ({preferences.AutoAcceptLevelFWHM})");
                    return true;
                }

                List<double> samples = GetSamples(population, i => { return i.Metadata.FWHM; });
                if (SamplesHaveData(samples)) {
                    TSLogger.Info("image grading: FWHM ->");
                    if (NearZero(fwhm) || !WithinAcceptableVariance(samples, fwhm, preferences.FWHMSigmaFactor, false)) {
                        TSLogger.Info("image grading: failed FWHM grading => NOT accepted");
                        return false;
                    }
                } else {
                    TSLogger.Warning("All comparison samples for FWHM don't have valid data, skipping FWHM grading");
                }
            }

            return true;
        }

        public bool GradeEccentricity(List<AcquiredImage> population) {
            if (!preferences.EnableGradeEccentricity) return true;

            double eccentricity = GetHocusFocusMetric(imageData.StarDetectionAnalysis, "Eccentricity");
            if (eccentricity == Double.NaN) {
                TSLogger.Warning("image grading: eccentricity grading is enabled but image doesn't have eccentricity metric.  Is Hocus Focus installed, enabled, and configured for star detection?");
            } else {
                if (preferences.AutoAcceptLevelEccentricity > 0 && eccentricity <= preferences.AutoAcceptLevelEccentricity) {
                    TSLogger.Info($"image grading: eccentricity auto accepted: actual ({eccentricity}) <= level ({preferences.AutoAcceptLevelEccentricity})");
                    return true;
                }

                List<double> samples = GetSamples(population, i => { return i.Metadata.Eccentricity; });
                if (SamplesHaveData(samples)) {
                    TSLogger.Info("image grading: eccentricity ->");
                    if (NearZero(eccentricity) || !WithinAcceptableVariance(samples, eccentricity, preferences.EccentricitySigmaFactor, false)) {
                        TSLogger.Info("image grading: failed eccentricity grading => NOT accepted");
                        return false;
                    }
                } else {
                    TSLogger.Warning("All comparison samples for eccentricity don't have valid data, skipping eccentricity grading");
                }
            }

            return true;
        }

        private List<double> GetSamples(List<AcquiredImage> population, Func<AcquiredImage, double> Sample) {
            List<double> samples = new List<double>();
            population.ForEach(i => samples.Add(Sample(i)));
            return samples;
        }

        private bool WithinAcceptableVariance(List<double> samples, double newSample, double sigmaFactor, bool positiveImprovement) {
            TSLogger.Info($"    samples={SamplesToString(samples)}");
            (double mean, double stddev) = SampleStandardDeviation(samples);

            if (preferences.AcceptImprovement) {
                if (positiveImprovement && newSample > mean) {
                    TSLogger.Info($"    mean={mean} sample={newSample} (acceptable: improved)");
                    return true;
                }
                if (!positiveImprovement && newSample < mean) {
                    TSLogger.Info($"    mean={mean} sample={newSample} (acceptable: improved)");
                    return true;
                }
            }

            double variance = Math.Abs(mean - newSample);
            TSLogger.Info($"    mean={mean} stddev={stddev} sample={newSample} variance={variance} sigmaX={sigmaFactor}");
            return variance <= (stddev * sigmaFactor);
        }

        public virtual double GetHocusFocusMetric(IStarDetectionAnalysis starDetectionAnalysis, string propertyName) {
            return starDetectionAnalysis.HasProperty(propertyName) ?
                (Double)starDetectionAnalysis.GetType().GetProperty(propertyName).GetValue(starDetectionAnalysis) :
                Double.NaN;
        }

        /// <summary>
        /// Determine the mean and the sample (not population) standard deviation of a set of samples.
        /// </summary>
        /// <param name="samples"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public (double, double) SampleStandardDeviation(List<double> samples) {
            if (samples == null || samples.Count < 3) {
                throw new Exception("must have >= 3 samples");
            }

            double mean = samples.Average();
            double sum = samples.Sum(d => Math.Pow(d - mean, 2));
            double stddev = Math.Sqrt((sum) / (samples.Count - 1));

            return (mean, stddev);
        }

        private bool SamplesHaveData(List<double> samples) {
            foreach (double sample in samples) {
                if (sample <= 0 || Double.IsNaN(sample)) {
                    return false;
                }
            }

            return true;
        }

        private bool NearZero(double value) {
            return Math.Abs(value) <= 0.001;
        }

        private double GetBinning() {
            try {
                string bin = imageData.MetaData?.Image?.Binning;
                if (string.IsNullOrEmpty(bin)) {
                    return 1;
                }

                return double.Parse(bin.Substring(0, 1));
            } catch (Exception) {
                return 1;
            }
        }

        private string SamplesToString(List<double> samples) {
            StringBuilder sb = new StringBuilder();
            samples.ForEach(s => sb.Append($"{s}, "));
            return sb.ToString();
        }

        private bool noGradingMetricsEnabled() {
            if (!preferences.EnableGradeStars && !preferences.EnableGradeHFR &&
                !preferences.EnableGradeFWHM && !preferences.EnableGradeEccentricity) {
                return true;
            }

            return false;
        }

        private bool enableGradeRMS() {
            // Disable RMS grading if running as a sync client since no guiding data will be available
            if (preferences.EnableGradeRMS && SyncManager.Instance.IsRunning && !SyncManager.Instance.IsServer) {
                return false;
            }

            return preferences.EnableGradeRMS;
        }
    }
}