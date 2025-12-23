using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himzo_watcher
{
    public static class Config
    {
        // --- NETWORK CONFIGURATION ---
        // If this is set to "CHANGE_ME", the program will list adapters and exit.
        // Copy the Name (GUID) from the console output into here.
        public const string AdapterGuid = "PLACEHOLDER";

        public const string TargetMachineIp = "PLACEHOLDER";

        // --- NOTIFICATION CONFIGURATION ---
        public const string SlackUrl = "PLACEHOLDER";
        public const string DiscordUrl = "PLACEHOLDER";

        // --- PARSING CONSTANTS ---
        public const int DataShift = 54; // Header offset (Ethernet+IP+TCP)
    }
}