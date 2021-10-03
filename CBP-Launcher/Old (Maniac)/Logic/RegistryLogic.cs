using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CBPLauncher.Logic
{
    ///
    /// Some of the registry logic is tied to getting paths, so can be found in Model-GetPaths instead of here
    ///
    
    public class RegistryLogic
    {
        /*public static RegistryKey RegPath;

        // strings
        private static string regPathDebug;
        public static string RegPathDebug => regPathDebug;

        public RegistryLogic()
        {
            ReadRegistry();
        }

        public static void ReadRegistry()
        {
            // apparently this is not a good method for this? use using instead? (but I don't know how to make that work with the bit-check :( ) https://stackoverflow.com/questions/1675864/read-a-registry-key

            try
            {
                if (Environment.Is64BitOperatingSystem) //I don't *fully* understand what's going on here (ported from stackexchange), but this block seems to be needed to prevent null value return due to 32/64 bit differences???
                {
                    RegPath = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
                }
                else
                {
                    RegPath = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
                }

                regPathDebug = "Debug: registry read as " + RegPath;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error with ReadRegistry:" + ex);
            }
        }*/
    }
}
