using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CBPLauncher.Logic
{
    public class PatchVersion
    {
        public string Branch { get; set; }      // main / pr / test / XBP
        public string Name { get; set; }        // e.g.: Alpha 10
        public string ShortName { get; set; }   // e.g.: a10
        public string RootPath { get; set; }    // path inside the alt versions workshop folder to get to this version's root
        public string PatchPath { get; set; }   // path inside the alt versions workshop folder to get to this version's patch file
        public string PatchHash { get; set; }   // SHA256 hash of this version
        public bool IsInstalled { get; set; }   // not sure if I actually want this (might be hard to update its state without reworking launcher architecture)

    }
    // TODO: need to create a TOML file (to distribute with CBP) that has all of this stuff
    /// e.g.
    //[[version]]
    //branch = "main"
    //name = "name1"
    //root = "path1"
    //patch_path = "asdf"
    //patch_hash = "asdf"
    //patch_LAA_path = "asdf"
    //patch_LAA_hash = "asf"

    //[[version]]
    //branch = "pr"
    //name = "name2"
    //root = "path2"
    //patch_path = "asdf"
    //patch_hash = "asdf"
    //patch_LAA_path = "asdf"
    //patch_LAA_hash = "asf"

    //[[version]]
    //branch = "test"
    //name = "name3"
    //root = "path3"
    //patch_path = "asdf"
    //patch_hash = "asdf"
    //patch_LAA_path = "asdf"
    //patch_LAA_hash = "asf"
}
