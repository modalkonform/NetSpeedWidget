namespace NetSpeedWidget;

/// <summary>Byte counters of one adapter, read at a point in time.</summary>
public readonly record struct TrafficSample(
  string AdapterId, long BytesReceived, long BytesSent, long TimestampMs);

/// <summary>Download and upload speed between two samples.</summary>
public readonly record struct TrafficRate(
  double DownloadBytesPerSecond, double UploadBytesPerSecond)
{
  public static readonly TrafficRate Zero = new(0, 0);

  /// <summary>
  /// Computes the rate between two samples, or zero when they can't be compared.
  /// </summary>
  public static TrafficRate Between(TrafficSample? previous, TrafficSample? current)
  {
    if (previous is not { } before || current is not { } after)
      return Zero;

    var received = after.BytesReceived - before.BytesReceived;
    var sent = after.BytesSent - before.BytesSent;
    var seconds = (after.TimestampMs - before.TimestampMs) / 1000.0;
    var comparable = before.AdapterId == after.AdapterId
      && received >= 0 && sent >= 0 && seconds > 0;

    return comparable ? new TrafficRate(received / seconds, sent / seconds) : Zero;
  }
}
