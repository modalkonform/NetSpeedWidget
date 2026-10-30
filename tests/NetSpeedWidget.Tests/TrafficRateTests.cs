namespace NetSpeedWidget.Tests;

public class TrafficRateTests
{
  [Fact]
  public void DividesCounterGrowthByElapsedSeconds()
  {
    var previous = new TrafficSample("wifi", BytesReceived: 1_000, BytesSent: 500,
      TimestampMs: 0);
    var current = new TrafficSample("wifi", BytesReceived: 3_000, BytesSent: 600,
      TimestampMs: 2_000);

    var rate = TrafficRate.Between(previous, current);

    Assert.Equal(1_000, rate.DownloadBytesPerSecond);
    Assert.Equal(50, rate.UploadBytesPerSecond);
  }

  [Fact]
  public void IsZero_WhenAdapterChanged()
  {
    var previous = new TrafficSample("wifi", 1_000, 500, 0);
    var current = new TrafficSample("ethernet", 9_000_000, 9_000, 1_000);

    Assert.Equal(TrafficRate.Zero, TrafficRate.Between(previous, current));
  }

  [Fact]
  public void IsZero_WhenCounterWentBackwards()
  {
    var previous = new TrafficSample("wifi", 5_000, 500, 0);
    var current = new TrafficSample("wifi", 100, 600, 1_000);

    Assert.Equal(TrafficRate.Zero, TrafficRate.Between(previous, current));
  }

  [Fact]
  public void IsZero_WhenNoTimeElapsed()
  {
    var previous = new TrafficSample("wifi", 1_000, 500, 1_000);
    var current = new TrafficSample("wifi", 2_000, 600, 1_000);

    Assert.Equal(TrafficRate.Zero, TrafficRate.Between(previous, current));
  }

  [Fact]
  public void IsZero_OnFirstSample()
  {
    var current = new TrafficSample("wifi", 2_000, 600, 1_000);

    Assert.Equal(TrafficRate.Zero, TrafficRate.Between(null, current));
  }

  [Fact]
  public void IsZero_WhenNoActiveAdapter()
  {
    var previous = new TrafficSample("wifi", 1_000, 500, 0);

    Assert.Equal(TrafficRate.Zero, TrafficRate.Between(previous, null));
  }
}
