using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace CBPLauncher.Logic
{
    public class LauncherStatusLogic
    {
        /*//private strings (etc)
        //private static bool launchEnabled = false;
        //private static string launchButtonText = "";
        //private static string launchStatusText = "";
        //private static Brush launchStatusColor = Brushes.White;

        //public strings (etc)
        public static bool LaunchEnabled { get; set; }
        public static string LaunchButtonText { get; set; }
        public static string LaunchStatusText { get; set; }
        public static Brush LaunchStatusColor { get; set; }

        ///
        /// The main launcher status thing - static so that it can be referenced easily by the other models
        ///

        public enum LauncherStatus
        {
            gettingReady,
            readyCBPEnabled,
            readyCBPDisabled,
            loadFailed,
            unloadFailed,
            installFailed,
            installingFirstTimeLocal,
            installingUpdateLocal,
            installingFirstTimeOnline,
            installingUpdateOnline,
            connectionProblemLoaded,
            connectionProblemUnloaded,
            installProblem
        }

        private static LauncherStatus _status;
        public static LauncherStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                switch (_status)
                {
                    case LauncherStatus.gettingReady:
                        LaunchStatusText = "Initializing..."; //copy string into, then that copy is bound by view
                        LaunchStatusColor = Brushes.White; // copy color into VM, then that copy is bound by view
                        DisableLaunchButton(); //this is used for the bool part of the CanExecute part of RelayCommands related to the launch button or w/e - still figuring out specifics
                        LaunchButtonText = "Launch Game";// LaunchButtonTest marked with empty comment doesn't necessarily need a string
                        break;
                    case LauncherStatus.readyCBPEnabled:
                        LaunchStatusText = "Ready: CBP enabled";
                        LaunchStatusColor = Brushes.LimeGreen;
                        EnableLaunchButton();
                        LaunchButtonText = "Launch Game";
                        break;
                    case LauncherStatus.readyCBPDisabled:
                        LaunchStatusText = "Ready: CBP disabled";
                        LaunchStatusColor = Brushes.Orange;
                        EnableLaunchButton();
                        LaunchButtonText = "Launch Game";
                        break;
                    case LauncherStatus.loadFailed:
                        LaunchStatusText = "Error: unable to load CBP";
                        LaunchStatusColor = Brushes.Red;
                        EnableLaunchButton();
                        LaunchButtonText = "Retry Load?";
                        break;
                    case LauncherStatus.unloadFailed:
                        LaunchStatusText = "Error: unable to unload CBP";
                        LaunchStatusColor = Brushes.Red;
                        EnableLaunchButton();
                        LaunchButtonText = "Retry Unload?";
                        break;
                    case LauncherStatus.installFailed:
                        LaunchStatusText = "Error: update failed";
                        LaunchStatusColor = Brushes.Red;
                        EnableLaunchButton();
                        LaunchButtonText = "Retry Update?";
                        break;
                    // I tried renaming the *Local to *Workshop and VS2019 literally did the opposite of that (by renaming what I just changed) instead of doing what it said it would wtf
                    case LauncherStatus.installingFirstTimeLocal:                       /// primary method: use workshop files;
                        LaunchStatusText = "Installing CBP from local files...";
                        LaunchStatusColor = Brushes.White;
                        DisableLaunchButton();
                        LaunchButtonText = "Launch Game";//
                        break;
                    case LauncherStatus.installingUpdateLocal:                          /// primary method: use workshop files;
                        LaunchStatusText = "Installing update from local files...";
                        LaunchStatusColor = Brushes.White;
                        DisableLaunchButton();
                        LaunchButtonText = "Launch Game";//
                        break;
                    case LauncherStatus.installingFirstTimeOnline:                      /// backup method: use online files;
                        LaunchStatusText = "Installing CBP from online files...";
                        LaunchStatusColor = Brushes.White;
                        DisableLaunchButton();
                        LaunchButtonText = "Launch Game";//
                        break;
                    case LauncherStatus.installingUpdateOnline:                         /// backup method: use online files; 
                        LaunchStatusText = "Installing update from online files...";
                        LaunchStatusColor = Brushes.White;
                        DisableLaunchButton();
                        LaunchButtonText = "Launch Game";//
                        break;
                    case LauncherStatus.connectionProblemLoaded:
                        LaunchStatusText = "Error: connectivity issue (CBP loaded)";
                        LaunchStatusColor = Brushes.OrangeRed;
                        EnableLaunchButton();
                        LaunchButtonText = "Launch game";
                        break;
                    case LauncherStatus.connectionProblemUnloaded:
                        LaunchStatusText = "Error: connectivity issue (CBP not loaded)";
                        LaunchStatusColor = Brushes.OrangeRed;
                        EnableLaunchButton();
                        LaunchButtonText = "Launch game";
                        break;
                    case LauncherStatus.installProblem:
                        LaunchStatusText = "Potential installation error";
                        LaunchStatusColor = Brushes.OrangeRed;
                        EnableLaunchButton();
                        LaunchButtonText = "Launch game";
                        break;
                    default:
                        break;
                }
            }
        }

        public LauncherStatusLogic()
        {
            //put automatic actions (things that always run at startup without user input) for the model here
            Status = LauncherStatus.gettingReady;

            //this stuff will run synchronously *here*, but the individual functions (etc) can still be called asynchronously in ViewModel, which is mostly what we care about
            //I assume there's a way to get this part running async too (e.g. just async call wrapper from vm?) but for our purposes it's not really needed
            //Function();
        }


        ///
        /// publicly accessible stuff so that outside can interact with model
        ///

        //is this even necessary? I can just set the status directly (and it will already error if incorrect)
        /*public async Task SetLauncherStatus(string status)
        {
            // 1) check if provided value is in LauncherStatus enum or not
            // 2) if yes, apply it
            // 3) if no, give error

            //check if input is
            if (Enum.IsDefined(typeof(LauncherStatus), status))
            {
                //valid
                MessageBox.Show("yes");
                return;
            }
            else
            {
                //not valid
                MessageBox.Show("no");
                return;
            }
        }*/

        /*public static void EnableLaunchButton() // in the viewmodel, ``LaunchCommand = new RelayCommand (o => { //launch }, o => {return model.LaunchEnabled});``
        {
            LaunchEnabled = true;
        }

        public static void DisableLaunchButton()
        {
            LaunchEnabled = false;
        }

        public static void ChangeLaunchStatus(LauncherStatus status)
        {
            Status = status;
        }*/

        ///
        /// additional private *functions* used by public ones
        ///

        //~currently nothing~
    }
}
