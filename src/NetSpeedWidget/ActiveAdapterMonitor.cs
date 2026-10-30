using System.Net;
using System.Net.NetworkInformation;

namespace NetSpeedWidget;

/// <summary>Reads the byte counters of the adapter Windows uses to reach the internet.</summary>
sealed class ActiveAdapterMonitor
{
  // Windows only uses this address to pick a route; nothing is sent to it.
  static readonly uint InternetAddress =
    BitConverter.ToUInt32(IPAddress.Parse("8.8.8.8").GetAddressBytes());

  uint? cachedIndex;
  NetworkInterface? cachedAdapter;

  /// <summary>Current counters of the active adapter, or null when offline.</summary>
  public TrafficSample? Read()
  {
    if (NativeMethods.GetBestInterface(InternetAddress, out var index) != 0)
      return null;

    if (index != cachedIndex)
    {
      cachedAdapter = FindByIndex(index);
      cachedIndex = index;
    }
    if (cachedAdapter is null)
      return null;

    var stats = cachedAdapter.GetIPStatistics();
    return new TrafficSample(
      cachedAdapter.Id, stats.BytesReceived, stats.BytesSent, Environment.TickCount64);
  }

  static NetworkInterface? FindByIndex(uint index)
  {
    return NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(adapter =>
      adapter.Supports(NetworkInterfaceComponent.IPv4)
      && adapter.GetIPProperties().GetIPv4Properties()?.Index == index);
  }
}
