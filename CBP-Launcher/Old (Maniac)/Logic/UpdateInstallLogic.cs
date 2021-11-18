using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static CBPLauncher.Logic.ArchiveLogic;
using static CBPLauncher.Logic.BasicIOLogic;
using static CBPLauncher.Logic.GetPathsLogic;
using static CBPLauncher.Logic.LauncherStatusLogic;
using static CBPLauncher.Logic.SettingsLogic;
using static CBPLauncher.Logic.VersionLogic;

namespace CBPLauncher.Logic
{
    public class UpdateInstallLogic
    {
        /*public UpdateInstallLogic()
        {
            // allow user to switch between CBP and unmodded, and if unmodded then CBP updating logic unneeded
            if (Properties.Settings.Default.DefaultCBP == true)
            {
                CheckForUpdates();
            };
            if (Properties.Settings.Default.DefaultCBP == false)
            {
                if (Properties.Settings.Default.CBPUnloaded == false && Properties.Settings.Default.CBPLoaded == true)
                {
                    UnloadCBP();
                }
                else
                {
                    Status = LauncherStatus.readyCBPDisabled;
                }
            }
        }

        public void CheckForUpdates()
        {
            try // without the try you can accidentally create online-only DRM whoops
            {
                UpdateVersionTextLatest("Checking latest version...");
                UpdateVersionTextInstalled("Checking installed version...");

                WebClient webClient = new WebClient();                                                               /// Moved this section from reference to here in order to display
                VersionLogic.Version onlineVersion = new VersionLogic.Version(webClient.DownloadString("http://mhloppy.com/CBP/version.txt")); /// latest available version as well as installed version

                if (Properties.Settings.Default.UsePrerelease == true)//pr
                {
                    onlineVersion = new VersionLogic.Version(webClient.DownloadString("http://mhloppy.com/CBP/versionpr.txt")); /// latest available version as well as installed version
                }

                UpdateVersionTextLatest("Latest CBP version: "
                     + VersionArray.versionStart[onlineVersion.major]
                     + VersionArray.versionMiddle[onlineVersion.minor]  ///space between major and minor moved to the string arrays in order to support the eventual 1.x release(s)
                     + VersionArray.versionEnd[onlineVersion.subMinor]  ///it's nice to have a little bit of forward thinking in the mess of code sometimes ::fingerguns::
                     + VersionArray.versionHotfix[onlineVersion.hotfix]);

                if (File.Exists(VersionFileCBP)) //If there's already a version.txt in the local-mods CBP folder, then...
                {
                    VersionLogic.Version localVersion = new VersionLogic.Version(File.ReadAllText(VersionFileCBP)); // this doesn't use UpdateLocalVersionNumber() because of the compare done below it - will break if replaced without modification

                    UpdateVersionTextInstalled("Installed CBP version: "
                                            + VersionArray.versionStart[localVersion.major]
                                            + VersionArray.versionMiddle[localVersion.minor]
                                            + VersionArray.versionEnd[localVersion.subMinor]
                                            + VersionArray.versionHotfix[localVersion.hotfix]);
                    try
                    {
                        if (onlineVersion.IsDifferentThan(localVersion))
                        {
                            InstallGameFiles(true, onlineVersion);
                        }
                        else
                        {
                            Status = LauncherStatus.readyCBPEnabled; //if the local version.txt matches the version found in the online file, then no patch required
                            Properties.Settings.Default.CBPLoaded = true;
                            Properties.Settings.Default.CBPUnloaded = false;
                            SaveSettings();
                        }
                    }
                    catch (Exception ex)
                    {
                        Status = LauncherStatus.installFailed;
                        MessageBox.Show($"Error installing patch files: {ex}");
                    }
                }

                // compatibility with a6c (maybe making it compatible was a mistake)
                else if (Directory.Exists(Path.Combine(LocalMods, "Community Balance Patch (Alpha 6c)")))
                {
                    InstallGameFiles(true, VersionLogic.Version.zero);
                }

                else
                {
                    InstallGameFiles(false, VersionLogic.Version.zero);
                }
            }
            catch (Exception ex)
            {
                if (Properties.Settings.Default.CBPLoaded == true)
                {
                    Status = LauncherStatus.connectionProblemLoaded;
                }
                else
                {
                    Status = LauncherStatus.connectionProblemUnloaded;
                }

                UpdateLocalVersionNumber();
                UpdateVersionTextLatest("Unable to check latest version");
                MessageBox.Show($"Error checking for updates. Maybe no connection could be established? {ex}");
            }
        }

        public void InstallGameFiles(bool _isUpdate, VersionLogic.Version _onlineVersion)
        {
            if (Properties.Settings.Default.CBPUnloaded == false)
            {
                if (Properties.Settings.Default.NoWorkshopFiles == false)
                {
                    // try using workshop files
                    try
                    {
                        // extra steps depending on whether this an update to existing install or first time install
                        if (_isUpdate)
                        {
                            Status = LauncherStatus.installingUpdateLocal;

                            // if archive setting is enabled, archive the old version; it looks for an unversioned CBP folder and has a separate check for a6c specifically
                            if (Properties.Settings.Default.CBPArchive == true)
                            {
                                // standard (non-a6c) archiving
                                if (Directory.Exists(Path.Combine(LocalPathCBP)))
                                {
                                    ArchiveNormal();
                                }

                                // compatibility with archiving a6c
                                else if (Directory.Exists(Path.Combine(LocalMods, "Community Balance Patch (Alpha 6c)")))
                                {
                                    ArchiveA6c();
                                }

                                else
                                {
                                    MessageBox.Show($"Archive setting is on, but there doesn't seem to be any compatible CBP install to archive.");
                                }
                            }
                            else
                            {
                                //just delete the old files instead
                            }
                        }
                        else
                        {
                            Status = LauncherStatus.installingFirstTimeLocal;
                        }

                        // perhaps this is a chance to use async, but the benefits are minor given the limited IO, and my half-hour attempt wasn't adequate to get it working
                        BasicIOLogic.DirectoryCopy(Path.Combine(WorkshopPathCBP, "Community Balance Patch"), Path.Combine(LocalPathCBP), true);

                        //temporary a7-structure copy feature for e.g. help.xml; should be redone (or at least re-examined) with CBPL re-do
                        // part pre-A: strings so that both A and C can access them
                        // part A: backup old files you're about to override
                        // part B: copy contents of <workshopIDfolder>/<Secondary> to RoN/data
                        // part C: (not here, search for PART C) when mod is unloaded, restore the old files

                        if (Properties.Settings.Default.OldFilesRenamed == false)
                        {
                            try
                            {
                                //PART A
                                File.Move(HelpXMLOrig, HelpXMLOrig + " (old)");
                                File.Move(InterfaceXMLOrig, InterfaceXMLOrig + " (old)");
                                File.Move(SetupwinXMLOrig, SetupwinXMLOrig + " (old)");
                                File.Move(PatriotsOrig, PatriotsOrig + " (original)");

                                Properties.Settings.Default.OldFilesRenamed = true;

                                try
                                {
                                    //PART B
                                    File.Copy(Path.Combine(LocalPathCBP, "Secondary", HelpXML), HelpXMLOrig);
                                    File.Copy(Path.Combine(LocalPathCBP, "Secondary", InterfaceXML), InterfaceXMLOrig);
                                    File.Copy(Path.Combine(LocalPathCBP, "Secondary", SetupwinXML), SetupwinXMLOrig);
                                    File.Copy(Path.Combine(WorkshopPathCBP, "CBP Setup GUI.exe"), PatriotsOrig);
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show("error with part B1 of the temp a7 logic:\n" + ex);
                                }


                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("error with the temp a7 logic:\n" + ex);
                            }
                            //end of temp
                        }

                        try
                        {
                            UpdateLocalVersionNumber();

                            Properties.Settings.Default.CBPLoaded = true;
                            Properties.Settings.Default.CBPUnloaded = false;
                            SaveSettings();

                            Status = LauncherStatus.readyCBPEnabled;
                        }
                        catch (Exception ex)
                        {
                            Status = LauncherStatus.loadFailed;
                            MessageBox.Show($"Error loading CBP: {ex}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Status = LauncherStatus.installFailed;
                        MessageBox.Show($"Error installing CBP from Workshop files: {ex}");
                    }
                }

                else if (Properties.Settings.Default.NoWorkshopFiles == true) // as of v0.3 release this option isn't even exposed to the user yet, but it'll be useful later
                {
                    // try using online files
                    try
                    {
                        WebClient webClient = new WebClient();
                        if (_isUpdate)
                        {
                            Status = LauncherStatus.installingUpdateOnline;
                        }
                        else
                        {
                            Status = LauncherStatus.installingFirstTimeOnline;
                            _onlineVersion = new VersionLogic.Version(webClient.DownloadString("http://mhloppy.com/CBP/version.txt")); /// maybe this should be ported to e.g. google drive as well? then again it's a 1KB file so I
                                                                                                                          /// guess the main concern would be server downtime (either temporary or long term server-taken-offline-forever)
                        }

                        webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadGameCompletedCallback);
                        webClient.DownloadFileAsync(new Uri("https://drive.google.com/uc?export=download&id=1hQYZtdsTDihFi33Cc_BisRUHdXvSy5o4"), GameZip, _onlineVersion); //a6c old one https://drive.google.com/uc?export=download&id=1usd0ihBy5HWxsD6UiabV3ohzGxB7SxDD
                    }
                    catch (Exception ex)
                    {
                        Status = LauncherStatus.installFailed;
                        MessageBox.Show($"Error retrieving patch files: {ex}");
                    }
                }

            }

            if (Properties.Settings.Default.CBPUnloaded == true)
            {
                try
                {
                    Directory.Move(Path.Combine(UnloadedModsPath, "Community Balance Patch"), Path.Combine(LocalPathCBP)); //this will still currently fail if the folder already exists though

                    try
                    {
                        //PART A
                        File.Move(HelpXMLOrig, HelpXMLOrig + " (old)");
                        File.Move(InterfaceXMLOrig, InterfaceXMLOrig + " (old)");
                        File.Move(SetupwinXMLOrig, SetupwinXMLOrig + " (old)");
                        File.Move(PatriotsOrig, PatriotsOrig + " (original)");

                        Properties.Settings.Default.OldFilesRenamed = true;

                        try
                        {
                            //PART B
                            File.Copy(Path.Combine(LocalPathCBP, "Secondary", HelpXML), HelpXMLOrig);
                            File.Copy(Path.Combine(LocalPathCBP, "Secondary", InterfaceXML), InterfaceXMLOrig);
                            File.Copy(Path.Combine(LocalPathCBP, "Secondary", SetupwinXML), SetupwinXMLOrig);
                            File.Copy(Path.Combine(WorkshopPathCBP, "CBP Setup GUI.exe"), PatriotsOrig);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("error with part B1 of the temp a7 logic:\n" + ex);
                        }


                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("error with the temp a7 logic:\n" + ex);
                    }

                    UpdateLocalVersionNumber();

                    Properties.Settings.Default.CBPLoaded = true;
                    Properties.Settings.Default.CBPUnloaded = false;
                    SaveSettings();

                    Status = LauncherStatus.readyCBPEnabled;
                }
                catch (Exception ex)
                {
                    Status = LauncherStatus.loadFailed;
                    MessageBox.Show($"Error loading CBP: {ex}");
                }
            }
        }

        private void DownloadGameCompletedCallback(object sender, AsyncCompletedEventArgs e)
        {
            try
            {
                var onlineVersion = ((VersionLogic.Version)e.UserState); //I literally don't know when to use var vs other stuff, but it works here so I guess it's fine???

                /// To make the online version be converted (not just the local version), need the near-duplicate code below (on this indent level).
                /// Vs reference, it separates the conversion to string until after displaying the version number,
                /// that way it displays e.g. "Alpha 6c" but actually writes e.g. "6.0.3" to version.txt so that future compares to that file will work
                string onlineVersionString = ((VersionLogic.Version)e.UserState).ToString();

                try
                {
                    ZipFile.ExtractToDirectory(GameZip, LocalMods);
                    File.Delete(GameZip); //extra file to local mods folder, then delete it after the extraction is done
                }
                catch (Exception ex)
                {
                    Status = LauncherStatus.installFailed;
                    File.Delete(GameZip); //without this, the .zip will remain if it successfully downloads but then errors while unzipping

                    // show a message asking user if they want to ignore the error (and unlock the launch button)
                    string message = $"If you've already installed CBP this error might be okay to ignore. It may occur if you have the CBP files but no version.txt file to read from, causing the launcher to incorrectly think CBP is not installed. "
                                     + "It's also *probably* okay to ignore this if you want to just play non-CBP for now."
                                     + "\n\nFull error: \n" + $"{ex}"
                                     + "\n\nIgnore error and continue?";
                    string title = "Error installing new patch files";

                    // if they say yes, then also ask if they want to write a new version.txt file where the mod is supposed to be installed:
                    if (MessageBox.Show(message, title, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        Status = LauncherStatus.installProblem;

                        string message2 = $"If you're very confident that CBP is actually installed and the problem is just the version.txt file, you can write a new file to resolve this issue."
                                          + "\n\nWould you like to write a new version.txt file?"
                                          + "\n(AVOID DOING THIS IF YOU'RE NOT SURE!!)";

                        string title2 = "Write new version.txt file?";

                        if (MessageBox.Show(message2, title2, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        {
                            // currently do nothing; explicitly preferred over using if-not-yes-then-return in case I change this later
                        }
                        else
                        {
                            return;
                        }
                    }
                    else
                    {
                        Environment.Exit(0); /// if they say no, then application is kill;
                    }                        /// Env.Exit used instead of App.Exit because it prevents more code from running
                }                            /// App.Exit was writing the new version file even if you said no on the prompt - maybe could be resolved, but this is okay I think

                File.WriteAllText(VersionFileCBP, onlineVersionString); // I thought this is where return would go, but it doesn't, so I evidently don't know what I'm doing

                UpdateLocalVersionNumber();

                Status = LauncherStatus.readyCBPEnabled;
                Properties.Settings.Default.CBPLoaded = true;
                SaveSettings();
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.installFailed;
                File.Delete(GameZip); //without this, the .zip will remain if it successfully downloads but then errors while unzipping
                MessageBox.Show($"Error installing new patch files: {ex}");
            }
        }

        public void UnloadCBP()
        {
            if (Properties.Settings.Default.CBPUnloaded == false)
            {
                try
                {
                    if (Process.GetProcessesByName("patriots").Length > 0)
                    {
                        MessageBox.Show($"CBP Setup GUI (patriots.exe) has to be closed before CBP can be unloaded.");
                    }
                    else
                    {
                        Directory.Move(LocalPathCBP, Path.Combine(UnloadedModsPath, "Community Balance Patch"));
                        Properties.Settings.Default.CBPUnloaded = true;
                        Properties.Settings.Default.CBPLoaded = false;
                        SaveSettings();

                        UpdateVersionTextInstalled("Installed CBP version: not loaded");

                        //PART C
                        File.Delete(HelpXMLOrig);
                        File.Delete(InterfaceXMLOrig);
                        File.Delete(SetupwinXMLOrig);
                        File.Delete(PatriotsOrig);
                        File.Move(HelpXMLOrig + " (old)", HelpXMLOrig);
                        File.Move(InterfaceXMLOrig + " (old)", InterfaceXMLOrig);
                        File.Move(SetupwinXMLOrig + " (old)", SetupwinXMLOrig);
                        File.Move(PatriotsOrig + " (original)", PatriotsOrig);

                        Properties.Settings.Default.OldFilesRenamed = false;
                        //end of c

                        Status = LauncherStatus.readyCBPDisabled;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error unloading mod: {ex}");
                    Status = LauncherStatus.unloadFailed;
                }

                if (Properties.Settings.Default.UnloadWorkshopToo == true)
                {
                    try
                    {
                        //unload workshop
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error unloading Workshop mod: {ex}");
                        Status = LauncherStatus.unloadFailed;
                    }
                }

            }
            else
            {
                MessageBox.Show($"CBP is already unloaded.");
            }
        }

        public void PlayButtonClick()
        {
            if (File.Exists(GameExe) && (Status == LauncherStatus.readyCBPEnabled || Status == LauncherStatus.readyCBPDisabled)) // make sure all "launch" button options are included here
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(GameExe) // if you do this wrong (I don't fully remember what "wrong" was) the game can launch weirdly e.g. errors, bad mod loads etc.
                {
                    WorkingDirectory = GamePathFinal //this change compared to reference app was suggested by VS itself - I'm assuming it's functionally equivalent at worst
                };
                Process.Start(startInfo);
                //DEBUG: Process.Start(gameExe);

                Application.Current.MainWindow.Close();
            }
            else if (Status == LauncherStatus.installFailed)
            {
                CheckForUpdates(); // because CheckForUpdates currently includes the logic for *all* installing/loading, it's used for both installFailed and loadFailed right now
            }
            else if (Status == LauncherStatus.loadFailed)
            {
                CheckForUpdates();
            }
            else if (Status == LauncherStatus.unloadFailed)
            {
                UnloadCBP();
            }
        }*/
    }
}
