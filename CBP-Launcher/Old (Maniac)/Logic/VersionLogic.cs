using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CBPLauncher.Logic.GetPathsLogic;

namespace CBPLauncher.Logic
{
    public class VersionLogic
    {
        /*private static string versionTextLatest;
        private static string versionTextInstalled;

        public static string VersionTextLatest => versionTextLatest;
        public static string VersionTextInstalled => versionTextInstalled;

        public VersionLogic()
        {
            //automagic
        }
        
        public static void UpdateVersionTextLatest(string version)
        {
            versionTextLatest = version;
        }

        public static void UpdateVersionTextInstalled(string version)
        {
            versionTextInstalled = version;
        }
        
        public static void UpdateLocalVersionNumber()
        {
            if (File.Exists(VersionFileCBP))
            {
                Version localVersion = new Version(File.ReadAllText(VersionFileCBP)); // moved to separate thing to reduce code duplication

                versionTextInstalled = "Installed CBP version: "
                                        + VersionArray.versionStart[localVersion.major]
                                        + VersionArray.versionMiddle[localVersion.minor]  ///space between major and minor moved to the string arrays in order to support the eventual 1.x release(s)
                                        + VersionArray.versionEnd[localVersion.subMinor]  ///it's nice to have a little bit of forward thinking in the mess of code sometimes ::fingerguns::
                                        + VersionArray.versionHotfix[localVersion.hotfix];
            }
            else
            {
                versionTextInstalled = "CBP not installed";
            }
        }

        public class VersionArray //this seems like an inelegant way to implement the string array? but I wasn't sure where else to put it (and have it work)
        {
            //cheeky bit of extra changes to convert the numerical/int based X.Y.Z into the versioning I already used before this launcher
            public static string[] versionStart = new string[11] { "not installed", "Pre-Alpha ", "Alpha ", "Beta ", "Release Candidate ", "1.", "2.", "3.", "4.", "5.", "6." }; // I am a fucking god figuring out how to properly use these arrays based on 10 fragments of 5% knowledge each
            public static string[] versionMiddle = new string[16] { "", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15" }; // I don't even know what "static" means in this context, I just know what I need to use it
            public static string[] versionEnd = new string[17] { "", "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p" }; //e.g. can optionally just skip the subminor by intentionally using [0]
            public static string[] versionHotfix = new string[20] { "", " (hotfix 1)", " (hotfix 2)", " (hotfix 3)", " (hotfix 4)", " (hotfix 5)", " (hotfix 6)", " (hotfix 7)", " (hotfix 8)", " (hotfix 9)" // 0-9 respectively
                                                              , " (special)" // 10
                                                              , " (PR1)", " (PR2)", " (PR3)", " (PR4)", " (PR5),", "(PR6)", "(PR7)", "(PR8)", "(PR9)" }; // 11-19 respectively
        }

        public struct Version
        {
            internal static Version zero = new Version(0, 0, 0, 0);

            // introduced a fourth tier of version numbering as well, since the naming convention doesn't work very well for subminor being used for the purpose of a hotfix
            public short major;    ///in reference these are private, but I want to refer to them in the version displayed to the user (which I'm converting to X.Y.Z numerical to e.g. "Alpha 6c")
            public short minor;    ///I feel obliged to point out that I have little/no frame of reference to know if this is "bad" to do so maybe this is a code sin and I'm just too naive to know
            public short subMinor;
            public short hotfix;

            internal Version(short _major, short _minor, short _subMinor, short _hotFix)
            {
                major = _major;
                minor = _minor;
                subMinor = _subMinor;
                hotfix = _hotFix;
            }

            internal Version(string _version)
            {
                string[] _versionStrings = _version.Split('.'); //version.txt uses an X.Y.Z version pattern e.g. 6.0.3, so break that up  on the "." to parse each value
                if (_versionStrings.Length != 4)
                {
                    major = 0;
                    minor = 0;
                    subMinor = 0;
                    hotfix = 0;
                    return; //if the version detected doesn't seem to follow the format expected, set detected version to 0.0.0
                }

                major = short.Parse(_versionStrings[0]);
                minor = short.Parse(_versionStrings[1]);
                subMinor = short.Parse(_versionStrings[2]);
                hotfix = short.Parse(_versionStrings[3]);
            }

            internal bool IsDifferentThan(Version _otherVersion) //check if version (from local version.txt file) matches online with online version.txt
            {
                if (major != _otherVersion.major)
                {
                    return true;
                }
                else
                {
                    if (minor != _otherVersion.minor)
                    {
                        return true;
                    }
                    else
                    {
                        if (subMinor != _otherVersion.subMinor) //presumably there's a more efficient / elegant way to lay this out e.g. run one check thrice, cycling major->minor->subminor->hotfix
                        {
                            return true;
                        }
                        else
                        {
                            if (hotfix != _otherVersion.hotfix)
                            {
                                return true;
                            }
                        }
                    }
                }
                return false; //detecting if they're different, so false = not different
            }

            public override string ToString()
            {
                return $"{major}.{minor}.{subMinor}.{hotfix}"; //because this is used for comparison, you can't put the conversion into e.g. "Alpha 6c" here or it will fail the version check above because of the format change
            }
        }*/
    }
}
