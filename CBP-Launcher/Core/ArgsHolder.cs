using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CBPLauncher.Core
{
    public static class ArgsHolder
    {
        public static string[] StartupArgs { get; private set; } = Array.Empty<string>();
        public static void SetStartupArgs(string[] args) => StartupArgs = args == null ? Array.Empty<string>() : (string[])args.Clone();
    }
}
