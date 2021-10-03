using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static CBPLauncher.Logic.GetPathsLogic;
using static CBPLauncher.Logic.LauncherStatusLogic;
using static CBPLauncher.Logic.VersionLogic;

namespace CBPLauncher.Logic
{
    public class ArchiveLogic
    {
        /*public static void ArchiveNormal()
        {
            try
            {
                //rename it after moving it, then check version and use that to rename the folder in the archived location
                Directory.Move(Path.Combine(LocalPathCBP), Path.Combine(ArchiveCBP, "Community Balance Patch"));

                VersionLogic.Version archiveVersion = new VersionLogic.Version(File.ReadAllText(Path.Combine(ArchiveCBP, "Community Balance Patch", "version.txt")));

                string archiveVersionNew = VersionArray.versionStart[archiveVersion.major]
                                         + VersionArray.versionMiddle[archiveVersion.minor]
                                         + VersionArray.versionEnd[archiveVersion.subMinor]
                                         + VersionArray.versionHotfix[archiveVersion.hotfix];

                Directory.Move(Path.Combine(ArchiveCBP, "Community Balance Patch"), Path.Combine(ArchiveCBP, "Community Balance Patch " + "(" + archiveVersionNew + ")"));
                MessageBox.Show(archiveVersionNew + " has been archived.");
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.loadFailed;
                MessageBox.Show($"Error archiving previous CBP version: {ex}");
            }
        }

        // can't use same version check because it uses a 3-digit identifier, not 4-digit, but since we know its name it's not too bad
        public static void ArchiveA6c()
        {
            try
            {
                //rename it after moving it
                Directory.Move(Path.Combine(LocalMods, "Community Balance Patch (Alpha 6c)"), Path.Combine(ArchiveCBP, "Community Balance Patch (Alpha 6c)"));
                MessageBox.Show("Alpha 6c has been archived.");
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.loadFailed;
                MessageBox.Show($"Error archiving previous CBP version (compatbility for a6c): {ex}");
            }
        }*/
    }
}
