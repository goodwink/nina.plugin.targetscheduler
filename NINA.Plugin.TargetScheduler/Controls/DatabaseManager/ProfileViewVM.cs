﻿using NINA.Core.MyMessageBox;
using NINA.Plugin.TargetScheduler.Database.Schema;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.ViewModel;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using RelayCommand = CommunityToolkit.Mvvm.Input.RelayCommand;
using RelayCommandParam = CommunityToolkit.Mvvm.Input.RelayCommand<object>;

namespace NINA.Plugin.TargetScheduler.Controls.DatabaseManager {

    public class ProfileViewVM : BaseVM {
        private DatabaseManagerVM managerVM;
        private ProfileMeta profile;
        private TreeDataItem parentItem;
        private List<Project> projects;

        public ProfileMeta Profile {
            get => profile;
            set {
                profile = value;
                RaisePropertyChanged(nameof(Profile));
            }
        }

        public List<Project> Projects {
            get => projects;
            set {
                projects = value;
                RaisePropertyChanged(nameof(Projects));
            }
        }

        public bool PasteEnabled {
            get => Clipboard.HasType(TreeDataType.Project);
        }

        public ProfileViewVM(DatabaseManagerVM managerVM, IProfileService profileService, TreeDataItem profileItem) : base(profileService) {
            this.managerVM = managerVM;
            Profile = (ProfileMeta)profileItem.Data;
            parentItem = profileItem;
            Projects = InitProjects(profileItem);

            ProfileSettingsCommand = new RelayCommand(ViewProfilePreferences);
            AddProjectCommand = new RelayCommand(AddProject);
            PasteProjectCommand = new RelayCommand(PasteProject);
            ImportCommand = new RelayCommand(DisplayProfileImport);
            ResetProfileCommand = new RelayCommand(ResetProfile);
            ViewProjectCommand = new RelayCommandParam(ViewProject);
            CopyProjectCommand = new RelayCommandParam(CopyProject);
        }

        private List<Project> InitProjects(TreeDataItem profileItem) {
            List<Project> projects = new List<Project>();
            foreach (TreeDataItem item in profileItem.Items) {
                projects.Add((Project)item.Data);
            }

            return projects;
        }

        private bool showProfileImportView = false;

        public bool ShowProfileImportView {
            get => showProfileImportView;
            set {
                showProfileImportView = value;
                RaisePropertyChanged(nameof(ShowProfileImportView));
            }
        }

        private ProfileImportViewVM profileImportVM;

        public ProfileImportViewVM ProfileImportVM {
            get => profileImportVM;
            set {
                profileImportVM = value;
                RaisePropertyChanged(nameof(ProfileImportVM));
            }
        }

        public ICommand ProfileSettingsCommand { get; private set; }
        public ICommand AddProjectCommand { get; private set; }
        public ICommand PasteProjectCommand { get; private set; }
        public ICommand ImportCommand { get; private set; }
        public ICommand ResetProfileCommand { get; private set; }
        public ICommand ViewProjectCommand { get; private set; }
        public ICommand CopyProjectCommand { get; private set; }

        private void ViewProfilePreferences() {
            managerVM.ViewProfilePreferences(Profile);
        }

        private void AddProject() {
            managerVM.AddNewProject(parentItem);
        }

        private void PasteProject() {
            managerVM.PasteProject(parentItem);
        }

        private void DisplayProfileImport() {
            ShowProfileImportView = !ShowProfileImportView;
            if (ShowProfileImportView) {
                ProfileImportVM = new ProfileImportViewVM(managerVM, parentItem, profileService);
            }
        }

        private void ResetProfile() {
            string message = $"Reset target completion (accepted and acquired counts) on all projects/targets under '{Profile.Name}'?  This cannot be undone.";
            if (MyMessageBox.Show(message, "Reset Target Completion?", MessageBoxButton.YesNo, MessageBoxResult.No) == MessageBoxResult.Yes) {
                managerVM.ResetProfile(parentItem);
            }
        }

        private void CopyProject(object obj) {
            Project project = obj as Project;
            if (project != null) {
                TreeDataItem item = Find(project);
                if (item != null) {
                    Clipboard.SetItem(item);
                    RaisePropertyChanged(nameof(PasteEnabled));
                }
            }
        }

        private void ViewProject(object obj) {
            Project project = obj as Project;
            if (project != null) {
                TreeDataItem item = Find(project);
                if (item != null) {
                    managerVM.NavigateTo(item);
                }
            }
        }

        private TreeDataItem Find(Project project) {
            foreach (TreeDataItem item in parentItem.Items) {
                if (((Project)item.Data).Id == project.Id) {
                    return item;
                }
            }

            return null;
        }
    }
}