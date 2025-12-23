using System;
using System.Linq;
using SharpPcap;
using SharpPcap.LibPcap;

public class NetworkMonitor
{
    private LibPcapLiveDevice _device;

    /// <summary>
    /// Lists all available network adapters to Console.
    /// </summary>
    public static void ListAdapters()
    {
        var devices = LibPcapLiveDeviceList.Instance;
        Console.WriteLine("Available Network Adapters:");
        Console.WriteLine("---------------------------");

        if (devices.Count == 0)
        {
            Console.WriteLine("No interfaces found! Make sure Npcap is installed.");
            return;
        }

        for (int i = 0; i < devices.Count; i++)
        {
            var dev = devices[i];
            Console.WriteLine($"{i}. {dev.Interface.FriendlyName}");
            Console.WriteLine($"   Name: {dev.Name}");
            Console.WriteLine($"   Description: {dev.Description}");
            Console.WriteLine();
        }
    }

    /// <summary>
    /// Opens the connection to the specific adapter.
    /// </summary>
    public bool OpenConnection(string adapterName, string targetIp)
    {
        try
        {
            var devices = LibPcapLiveDeviceList.Instance;

            // Find the device by Name (GUID)
            _device = devices.FirstOrDefault(d => d.Name.Equals(adapterName, StringComparison.OrdinalIgnoreCase));

            if (_device == null)
            {
                Console.WriteLine($"Error: Adapter '{adapterName}' not found.");
                return false;
            }

            // Open the device
            _device.Open(new DeviceConfiguration
            {
                Mode = DeviceModes.Promiscuous,
                ReadTimeout = 20
            });

            Console.WriteLine($"Listening on: {_device.Description}");

            // Set the Filter
            string filter = $"src host {targetIp} and tcp";
            _device.Filter = filter;
            Console.WriteLine($"Filter set: {filter}");

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"CRITICAL ERROR opening adapter: {ex.Message}");
            return false;
        }
    }

    public void StartCapture(Action<RawCapture> onPacketReceived)
    {
        if (_device == null || !_device.Opened)
        {
            throw new InvalidOperationException("Device not opened. Call OpenConnection first.");
        }

        _device.OnPacketArrival += (sender, e) => onPacketReceived(e.GetPacket());

        _device.StartCapture();
        Console.WriteLine("HimzoWatcher C# - Status monitoring started...");
    }

    public void Stop()
    {
        if (_device != null)
        {
            _device.StopCapture();
            _device.Close();
        }
    }
}