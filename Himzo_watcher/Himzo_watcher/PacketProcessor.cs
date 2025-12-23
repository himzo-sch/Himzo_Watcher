using SharpPcap;
using System;

namespace Himzo_watcher
{
    public class PacketProcessor
    {
        private int _prevState = -1; // Start at -1 to ensure first packet triggers log

        // Event to send the code to Main (for Slack/Discord)
        public event Action<int>? OnStateChanged;

        public void ProcessPacket(RawCapture packet)
        {
            var data = packet.Data;
            int offset = Config.DataShift;

            // 1. Check for the minimum length needed for STATUS (offset + 8 is the index, so length must be +9)
            if (data.Length < offset + 9) return;

            // 2. Check if it's a "Stitch Count" packet (Condition: Bytes at +3 is 'I' and +7 is 'G') - only for debugging for now
            if (data[offset + 3] == 73 && data[offset + 7] == 71)
            {
                /*
                // Reconstruct the 32-bit integer from 4 bytes (Little Endian)
                int stitchCount = (data[offset + 15]) |
                                  (data[offset + 16] << 8) |
                                  (data[offset + 17] << 16) |
                                  (data[offset + 18] << 24);

                // Apply the offset this comes from the og code
                stitchCount -= 1024;

                // Debug output
                Console.WriteLine($"[Debug] Stitch Count: {stitchCount}");
                //*/

                return; // It's a stitch packet, so we stop here
            }

            // 3. If not stitch, check Machine Status
            int newState = -1;

            // Check byte at Offset + 7
            switch (data[offset + 7])
            {
                case 68: // 'D'
                    if (data[offset + 8] == 68) newState = 68; // 'D' -> RUNNING
                    else if (data[offset + 8] == 70) newState = 4;  // 'F' -> END
                    break;

                case 83: // 'S'
                    switch (data[offset + 8])
                    {
                        case 69: newState = 3; break; // 'E' -> ERROR
                        case 77: newState = 4; break; // 'M' -> END
                        case 78: newState = 0; break; // 'N' -> STOP SWITCH
                        case 83: newState = 1; break; // 'S' -> NEEDLE STOP
                        case 84: newState = 2; break; // 'T' -> THREAD BREAK
                    }
                    break;
            }

            // 4. Process State Change
            if (newState != -1 && newState != _prevState)
            {
                _prevState = newState;

                // Print to Console immediately (CMD Output)
                PrintConsoleStatus(newState);

                // Trigger notification event (for Slack/Discord)
                OnStateChanged?.Invoke(newState);
            }
        }

        public static string PrintConsoleStatus(int code)
        {
            string msg = "Ismeretlen hiba";
            switch (code)
            {
                case 3: msg = "Gephiba (???)"; break;
                case 4: msg = "Kesz a himzes"; break;
                case 0: msg = "Megallitva"; break;
                case 1: msg = "Elore beallitott STOP"; break;
                case 2: msg = "Szalszakadas!"; break;
                case 68: msg = "Elinditva"; break;
            }

            return msg;
        }
    }
}