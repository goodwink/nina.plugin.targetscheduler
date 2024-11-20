﻿using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using System.Text;

namespace NINA.Plugin.TargetScheduler.Database.Schema {

    public class ExposurePlan : IExposureCounts, INotifyPropertyChanged {
        [Key] public int Id { get; set; }
        [Required] public string profileId { get; set; }
        [NotMapped] private int exposureTemplateId;
        [Required] public double exposure { get; set; }

        public int desired { get; set; }
        public int acquired { get; set; }
        public int accepted { get; set; }

        [ForeignKey("ExposureTemplate")]
        public int ExposureTemplateId {
            get { return exposureTemplateId; }
            set {
                exposureTemplateId = value;
                RaisePropertyChanged(nameof(ExposureTemplateId));
            }
        }

        public virtual ExposureTemplate ExposureTemplate { get; set; }

        [ForeignKey("Target")] public int TargetId { get; set; }
        public virtual Target Target { get; set; }

        [NotMapped]
        public string ProfileId {
            get { return profileId; }
            set {
                profileId = value;
                RaisePropertyChanged(nameof(ProfileId));
            }
        }

        [NotMapped]
        public double Exposure {
            get { return exposure; }
            set {
                exposure = value;
                RaisePropertyChanged(nameof(Exposure));
            }
        }

        [NotMapped]
        public int Desired {
            get { return desired; }
            set {
                desired = value;
                RaisePropertyChanged(nameof(Desired));
            }
        }

        [NotMapped]
        public int Acquired {
            get { return acquired; }
            set {
                acquired = value;
                RaisePropertyChanged(nameof(Acquired));
            }
        }

        [NotMapped]
        public int Accepted {
            get { return accepted; }
            set {
                accepted = value;
                RaisePropertyChanged(nameof(Accepted));
            }
        }

        [NotMapped]
        public double PercentComplete { get; set; }

        public ExposurePlan() {
        }

        public ExposurePlan(string profileId) {
            ProfileId = profileId;
            Exposure = -1;
            Desired = 1;
            Acquired = 0;
            Accepted = 0;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null) {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ExposurePlan GetPasteCopy(string newProfileId) {
            ExposurePlan exposurePlan = new ExposurePlan();

            exposurePlan.profileId = newProfileId;
            exposurePlan.ExposureTemplateId = this.ExposureTemplateId;
            exposurePlan.exposure = exposure;
            exposurePlan.desired = desired;
            exposurePlan.acquired = 0;
            exposurePlan.accepted = 0;

            return exposurePlan;
        }

        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"ProfileId: {ProfileId}");
            sb.AppendLine($"TargetId: {TargetId}");
            sb.AppendLine($"ExposureTemplate: {ExposureTemplate}");
            sb.AppendLine($"Exposure: {Exposure}");
            sb.AppendLine($"Desired: {Desired}");
            sb.AppendLine($"Acquired: {Acquired}");
            sb.AppendLine($"Accepted: {Accepted}");

            return sb.ToString();
        }
    }

    public interface IExposureCounts {
        int Desired { get; set; }
        int Accepted { get; set; }
        int Acquired { get; set; }
    }
}