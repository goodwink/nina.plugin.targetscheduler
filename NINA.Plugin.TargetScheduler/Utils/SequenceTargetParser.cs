﻿using Newtonsoft.Json;
using NINA.Astrometry;
using NINA.Plugin.TargetScheduler.Shared.Utility;
using System;
using System.IO;
using System.Text;

namespace NINA.Plugin.TargetScheduler.Util {

    /// <summary>
    /// This is really ugly but unfortunately you can't just deserialize this type of serialized object directly since
    /// you get a NPE.  My guess is that NINA does it only in the context of having a real lat/long which makes it possible
    /// to construct a NINA.Astrometry.InputCoordinates.
    /// </summary>
    public class SequenceTargetParser {

        public static SequenceTarget GetSequenceTarget(string pathToFile) {
            string json = GetJson(pathToFile);
            return JsonConvert.DeserializeObject<SequenceTarget>(json);
        }

        private static string GetJson(string pathToFile) {
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            bool inObject = false;
            foreach (string line in File.ReadAllLines(pathToFile)) {
                if (line.Contains("TargetName") || line.Contains("PositionAngle")) {
                    sb.Append(line);
                }

                if (!inObject && line.Contains("InputCoordinates")) {
                    inObject = true;
                    continue;
                }

                if (inObject && line.Contains("}")) {
                    sb.Append("}");
                    return sb.ToString();
                }

                if (inObject && !line.Contains("$")) {
                    sb.Append(line);
                }
            }

            TSLogger.Error($"failed to parse sequence target json from file (key lines not found): {pathToFile}");
            throw new Exception($"failed to parse sequence target json from file (key lines not found): {pathToFile}");
        }

        private SequenceTargetParser() {
        }
    }

    public class SequenceTarget {
        private int RAHours { get; set; }
        private int RAMinutes { get; set; }
        private double RASeconds { get; set; }
        private bool NegativeDec { get; set; }
        private int DecDegrees { get; set; }
        private int DecMinutes { get; set; }
        private double DecSeconds { get; set; }

        public string TargetName { get; set; }
        public double Rotation { get; set; }

        public SequenceTarget(int raHours, int raMinutes, double raSeconds,
            bool negativeDec, int decDegrees, int decMinutes, double decSeconds,
            string targetName, double positionAngle) {
            RAHours = raHours;
            RAMinutes = raMinutes;
            RASeconds = raSeconds;
            NegativeDec = negativeDec;
            DecDegrees = decDegrees;
            DecMinutes = decMinutes;
            DecSeconds = decSeconds;
            TargetName = targetName;
            Rotation = positionAngle;
        }

        public Coordinates GetCoordinates() {
            string hms = string.Format("{0:00}:{1:00}:{2:00}", RAHours, RAMinutes, RASeconds);
            string dms = string.Format("{0:00}:{1:00}:{2:00}", DecDegrees, DecMinutes, DecSeconds);
            return new Coordinates(AstroUtil.HMSToDegrees(hms), AstroUtil.DMSToDegrees(dms), Epoch.J2000, Coordinates.RAType.Degrees);
        }
    }
}