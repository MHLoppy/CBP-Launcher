using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using CBPLauncher.Core;
using CBPLauncher.Logic;
using static CBPLauncher.Logic.LauncherStatusLogic;

namespace CBPLauncher.MVVM.ViewModel
{
    public class ViewModel_Primary : ObservableObject
    {
        /*//strings (etc)
        public string LaunchButtonTextVM
        {
            get => LaunchButtonText;
            set
            {
                LaunchButtonText = value;
                OnPropertyChanged();
            }
        }

        public string LaunchStatusTextVM
        {
            get => LaunchStatusText;
            set
            {
                LaunchStatusText = value;
                OnPropertyChanged();
            }
        }

        public Brush LaunchStatusColorVM
        {
            get => LaunchStatusColor;
            set
            {
                LaunchStatusColor = value;
                OnPropertyChanged();
            }
        }

        public bool LaunchEnabledVM
        {
            get => LaunchEnabled;
            set
            {
                LaunchEnabled = value;
                OnPropertyChanged();
            }
        }

        //commands
        public RelayCommand LaunchCommand { get; set; }
        public RelayCommand ManualLoadCBP { get; set; }
        public RelayCommand ManualUnloadCBP { get; set; }
        public RelayCommand ResetSettings { get; set; }


        //testing commands
        public RelayCommand LaunchEnableCommand { get; set; }
        public RelayCommand LaunchDisableCommand { get; set; }
        public RelayCommand SetStatusCommand { get; set; }
        public RelayCommand CheckStatusCommand { get; set; }
        public RelayCommand CheckColorCommand { get; set; }
        public RelayCommand UpdateColorCommand { get; set; }

        public ViewModel_Primary()
        {
            BasicIOLogic basicIOLogic = new BasicIOLogic();

            LauncherStatusLogic launcherStatusLogic = new LauncherStatusLogic();
            SettingsLogic settingsLogic = new SettingsLogic();
            RegistryLogic registryLogic = new RegistryLogic();

            GetPathsLogic getPathsLogic = new GetPathsLogic();
            VersionLogic versionLogic = new VersionLogic();

            ArchiveLogic archiveLogic = new ArchiveLogic();
            UpdateInstallLogic updateInstallLogic = new UpdateInstallLogic();

            //command definitions
            LaunchCommand = new RelayCommand(o =>
            {
                MessageBox.Show("hoi, oi'm tebby");//obviously this should instead do the launch function

                updateInstallLogic.PlayButtonClick();
            }, o =>
            {
                return LaunchEnabled;
            });

            ManualLoadCBP = new RelayCommand(o =>
            {
                updateInstallLogic.CheckForUpdates();
            });//currently always enabled

            ManualUnloadCBP = new RelayCommand(o =>
            {
                updateInstallLogic.UnloadCBP();
            });

            ResetSettings = new RelayCommand(o =>
            {
                Properties.Settings.Default.Reset();

                MessageBox.Show($"Settings reset. Default settings will be loaded the next time the program is loaded.");
            });




            //test command definitions
            LaunchEnableCommand = new RelayCommand(o =>
            {
                EnableLaunchButton();
            });

            LaunchDisableCommand = new RelayCommand(o =>
            {
                DisableLaunchButton();
            });

            SetStatusCommand = new RelayCommand(o =>
            {
                ChangeLaunchStatus(LauncherStatus.readyCBPEnabled);
            });

            CheckStatusCommand = new RelayCommand(o =>
            {
                MessageBox.Show(Status.ToString());
            });

            CheckColorCommand = new RelayCommand(o =>
            {
                MessageBox.Show("model: " + LaunchStatusColor.ToString());
                MessageBox.Show("viewmodel: " + LaunchStatusColorVM.ToString());
            });

            UpdateColorCommand = new RelayCommand(o =>
            {
                LaunchStatusColorVM = LaunchStatusColor;
            });
        }*/
    }
}
