using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpPcap;
using SharpPcap.LibPcap;

namespace Himzo_watcher
{
    internal class Program
    {
        static void Main()
        {
            Console.WriteLine("HimzoWatcher C# - HappyLAN Machine State Extractor");

            // 1. Auto-detect if we need to help the user find the adapter
            if (Config.AdapterGuid == "CHANGE_ME" || string.IsNullOrEmpty(Config.AdapterGuid))
            {
                Console.WriteLine("Configuration missing! Listing adapters...");
                NetworkMonitor.ListAdapters();
                Console.WriteLine("\nACTION REQUIRED: Copy the 'Name' (GUID) of your adapter into Config.cs and rebuild.");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                return;
            }

            // 2. Initialize components
            var monitor = new NetworkMonitor();
            var processor = new PacketProcessor();

            // 3. Hook up the logic: When Processor detects change -> Do Notification
            processor.OnStateChanged += async (stateCode) =>
            {
                string msg = PacketProcessor.PrintConsoleStatus(stateCode);
                await Messager.SendMessageAsync(msg);
            };

            // 4. Start Listening
            if (monitor.OpenConnection(Config.AdapterGuid, Config.TargetMachineIp))
            {
                try
                {
                    // Pass every raw packet to the processor
                    monitor.StartCapture(packet => processor.ProcessPacket(packet));

                    Console.ReadLine();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during capture: {ex.Message}");
                }
                finally
                {
                    monitor.Stop();
                }
            }
            else
            {
                Console.WriteLine("Failed to open connection. Check GUID and IP.");
                Console.ReadLine();
            }
        }
    }
}
