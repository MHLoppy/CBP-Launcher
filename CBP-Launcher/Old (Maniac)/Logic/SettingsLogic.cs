using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CBPLauncher.Logic
{
    public class SettingsLogic
    {
        /*// "UI settings", like checkboxes
        private static bool defaultCBP;
        private static bool defaultCBPPR;

        public static bool DefaultCBP => defaultCBP;
        public static bool DefaultCBPPR => defaultCBPPR;
        
        public SettingsLogic()
        {
            // new versions always default to upgraderequired true, so new versions always upgrade settings and then turn the flag (bool) for doing this off
            if (Properties.Settings.Default.UpgradeRequired == true)
            {
                UpgradeSettings();
                SaveSettings();
            }

            CheckDefaults();
        }

        // probably not a net positive to make stuff like this async
        public static void SaveSettings()
        {
            Properties.Settings.Default.Save();
        }

        public void UpgradeSettings()
        {
            Properties.Settings.Default.Upgrade();
            Properties.Settings.Default.UpgradeRequired = false;
        }

        // in theory I could just manipulate these settings directly from the viewmodel (sort of as if settings were a model), but not doing that seems like better practise?
        public static void CheckDefaults()
        {
            // default to loading CBP (true) or not (false)
            if (Properties.Settings.Default.DefaultCBP == true)
            {
                defaultCBP = true;
            }
            else if (Properties.Settings.Default.DefaultCBP == false)
            {
                defaultCBP = false;
            }

            // when using CBP, default to using the pre-release version (true) or not (false)
            if (Properties.Settings.Default.UsePrerelease == true)
            {
                defaultCBPPR = true;
            }
            else if (Properties.Settings.Default.UsePrerelease == false)
            {
                defaultCBPPR = false;
            }
        
        }*/
    }
}
