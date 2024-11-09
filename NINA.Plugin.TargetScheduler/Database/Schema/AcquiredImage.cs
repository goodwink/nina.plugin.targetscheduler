﻿using Newtonsoft.Json;
using NINA.Plugin.TargetScheduler.Shared.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Text;

namespace NINA.Plugin.TargetScheduler.Database.Schema {

    public class AcquiredImage {
        [Key] public int Id { get; set; }
        public string profileId { get; set; }
        [Required] public int ProjectId { get; set; }
        [Required] public int TargetId { get; set; }
        public long acquiredDate { get; set; }
        [Required] public string FilterName { get; set; }
        [Required] public int accepted { get; set; }
        public string rejectreason { get; set; }
        internal string _metadata { get; set; }

        [NotMapped]
        public string ProfileId {
            get { return profileId == null ? "" : profileId; }
            set {
                profileId = value;
            }
        }

        [NotMapped]
        public DateTime AcquiredDate {
            get { return Common.UnixSecondsToDateTime(acquiredDate); }
            set { acquiredDate = Common.DateTimeToUnixSeconds(value); }
        }

        [NotMapped]
        public bool Accepted {
            get { return accepted == 1; }
            set {
                accepted = value ? 1 : 0;
            }
        }

        [NotMapped]
        public string RejectReason {
            get { return rejectreason == null ? "" : rejectreason; }
            set {
                rejectreason = value;
            }
        }

        [NotMapped]
        public ImageMetadata Metadata {
            get {
                return JsonConvert.DeserializeObject<ImageMetadata>(_metadata);
            }
            set {
                _metadata = JsonConvert.SerializeObject(value);
            }
        }

        public virtual List<ImageData> Images { get; set; }

        public AcquiredImage() {
        }

        public AcquiredImage(ImageMetadata imageMetadata) {
            this.Metadata = imageMetadata;
        }

        public AcquiredImage(string profileId, int projectId, int targetId, DateTime acquiredDate, string filterName, bool accepted, string rejectReason, ImageMetadata imageMetadata) {
            this.ProfileId = profileId;
            this.ProjectId = projectId;
            this.TargetId = targetId;
            this.AcquiredDate = acquiredDate;
            this.FilterName = filterName;
            this.Accepted = accepted;
            this.RejectReason = rejectReason;
            this.Metadata = imageMetadata;
        }

        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"ProfileId: {ProfileId}");
            sb.AppendLine($"AcquiredDate: {AcquiredDate}");
            sb.AppendLine($"FilterName: {FilterName}");
            sb.AppendLine($"Accepted: {Accepted}");
            sb.AppendLine($"RejectReason: {RejectReason}");
            sb.AppendLine($"Metadata: {_metadata}");

            return sb.ToString();
        }
    }

    internal class AcquiredImageConfiguration : EntityTypeConfiguration<AcquiredImage> {

        public AcquiredImageConfiguration() {
            HasKey(x => new { x.Id });
            Property(x => x._metadata).HasColumnName("metadata");
            HasMany(x => x.Images)
                .WithRequired(i => i.AcquiredImage)
                .HasForeignKey(i => i.AcquiredImageId);
        }
    }
}