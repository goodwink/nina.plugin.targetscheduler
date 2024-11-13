﻿using NINA.Plugin.TargetScheduler.Database.Schema;
using NINA.Profile.Interfaces;

namespace NINA.Plugin.TargetScheduler.Database {

    public class SchedulerPlanLoader {
        private IProfile activeProfile;
        private string profileId;

        public SchedulerPlanLoader(IProfile activeProfile) {
            this.activeProfile = activeProfile;
            profileId = activeProfile.Id.ToString();
        }

        public ProfilePreference GetProfilePreferences() {
            return GetProfilePreferences(new SchedulerDatabaseInteraction().GetContext());
        }

        public ProfilePreference GetProfilePreferences(SchedulerDatabaseContext context) {
            using (context) {
                return context.GetProfilePreference(profileId, true);
            }
        }

        /* TODO
        public List<IPlanProject> LoadActiveProjects(SchedulerDatabaseContext context) {
            List<Project> projects = null;

            using (context) {
                try {
                    projects = context.GetActiveProjects(profileId);
                } catch (Exception ex) {
                    throw ex; // let the caller decide how to handle
                }

                if (projects == null || projects.Count == 0) {
                    TSLogger.Warning("no projects are active at planning time");
                    return null;
                }

                bool haveActiveTargets = false;
                ProfilePreference profilePreference = GetProfilePreferences();

                foreach (Project project in projects) {
                    ExposureCompletionHelper helper = new ExposureCompletionHelper(project.EnableGrader, profilePreference.ExposureThrottle);
                    foreach (Target target in project.Targets) {
                        if (target.Enabled) {
                            foreach (ExposurePlan plan in target.ExposurePlans) {
                                if (helper.RemainingExposures(plan) > 0) {
                                    haveActiveTargets = true;
                                    break;
                                }
                            }
                        }
                        if (haveActiveTargets) { break; }
                    }

                    if (haveActiveTargets) { break; }
                }

                if (!haveActiveTargets) {
                    TSLogger.Warning("no targets with exposure plans are active for active projects at planning time");
                    return null;
                }

                List<IPlanProject> planProjects = new List<IPlanProject>();
                foreach (Project project in projects) {
                    ExposureCompletionHelper helper = new ExposureCompletionHelper(project.EnableGrader, profilePreference.ExposureThrottle);
                    PlanProject planProject = new PlanProject(activeProfile, project, helper);
                    planProjects.Add(planProject);
                }

                return planProjects;
            }
        }*/
    }
}