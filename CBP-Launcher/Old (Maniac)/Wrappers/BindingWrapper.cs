using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using CBPLauncher.Core;
using CBPLauncher.Logic;

namespace CBPLauncher.Wrappers
{
    public class BindingWrapper : ObservableObject
    {
        /// public LogicClassName AbbreviatedName { get; set; }
        /// public AnotherLogicClassName AnotherAbbreviatedName { get; set; }
        /// etc
        ///  
        /// public BindingWrapper()
        /// {
        ///     AbbreviatedName = new LogicClassName();
        ///     AnotherAbbreviatedname = new AnotherLogicClassName
        /// }
        /// XAML datacontext is BindingWrapper, but individual bindings are to AbbreviatedName.ExampleCommand and AbbreviatedName.ExampleString
        
        /*public ArchiveLogic ArchiveL { get; set; }
        public BasicIOLogic BasicIOL { get; set; }
        public GetPathsLogic GetPathsL { get; set; }
        public LauncherStatusLogic LauncherStatusL { get; set; }
        public RegistryLogic RegistryL { get; set; }
        public SettingsLogic SettingsL { get; set; }
        public UpdateInstallLogic UpdateInstallL { get; set; }
        public VersionLogic VersionL { get; set; }

        public BindingWrapper()
        {
            // because this is a staticresource, need to prevent it imploding
            // see also here for more context: https://stackoverflow.com/a/69275132/16367940
            if (IsInDesignMode() == false)
            {
                ArchiveL = new ArchiveLogic();
                BasicIOL = new BasicIOLogic();
                GetPathsL = new GetPathsLogic();
                LauncherStatusL = new LauncherStatusLogic();
                RegistryL = new RegistryLogic();
                SettingsL = new SettingsLogic();
                UpdateInstallL = new UpdateInstallLogic();
                VersionL = new VersionLogic();
            }
            else
            {
                //designtime baybeee
            }

        }

        private DependencyObject dummy = new DependencyObject();

        private bool IsInDesignMode()
        {
            return DesignerProperties.GetIsInDesignMode(dummy);
        }*/
    }
}
