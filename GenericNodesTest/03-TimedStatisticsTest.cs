using System;
using System.Diagnostics;
using System.Threading.Tasks;
using LogicModule.Nodes.TestHelper;
using LogicModule.ObjectModel;
using NUnit.Framework;

namespace Recomedia_de.Logic.Generic.Test
{
  [TestFixture]
  class TimedStatisticsTest
  {
    private readonly INodeContext context;
    private TimedStatistics node;
    private MockSchedulerService schedulerService;

    public TimedStatisticsTest()
    {
      context = TestNodeContext.Create();
      schedulerService = (MockSchedulerService)context.GetService<ISchedulerService>();
    }

    [SetUp]
    public void TimedStatisticsTestSetUp()
    {
      node = new TimedStatistics(context);
    }

    [TearDown]
    public void TimedStatisticsTestTearDown()
    {
      node = null;
    }

    [Test]
    public void ValidateTest()
    {
      // Default values must validate without error
      var result = node.Validate("en");
      Assert.False(result.HasError);

      // Statistics over less than 5 seconds forbidden
      node.mConsideredTime.Value = new TimeSpan(0, 0, 0, 0, 4999);
      result = node.Validate("en");
      Assert.True(result.HasError);
      Assert.AreEqual("The considered timespan cannot be less than 5s (or 0 for unlimited).",
                      result.Message);
      result = node.Validate("de");
      Assert.True(result.HasError);
      Assert.AreEqual("Der Betrachtungszeitraum kann nicht kleiner als 5s sein (oder" +
                      " 0 für beliebig lange).", result.Message);

      // Statistics over 10s
      node.mConsideredTime.Value = new TimeSpan(0, 0, 10);
      // Try to update at least every 5.01s
      node.mUpdateTime.Value = new TimeSpan(0, 0, 0, 5, 100);
      result = node.Validate("en");
      Assert.True(result.HasError);
      Assert.AreEqual("The update period cannot be more than half of the considered timespan" +
                      " (or 0 for no updates between received telegrams).", result.Message);
      result = node.Validate("de");
      Assert.True(result.HasError);
      Assert.AreEqual("Das Zeitintervall für Aktualisierung kann nicht größer als die Hälfte" +
                      " des Betrachtungszeitraums sein (oder 0 für Aktualisierung nur bei" +
                      " eingehenden Telegrammen).", result.Message);
      // Try to update at least every 0.499s
      node.mUpdateTime.Value = new TimeSpan(0, 0, 0, 0, 499);
      result = node.Validate("en");
      Assert.True(result.HasError);
      Assert.AreEqual("The update period cannot be less than 0.5s (or 0 for no updates between" +
                      " received telegrams).", result.Message);
      result = node.Validate("de");
      Assert.True(result.HasError);
      Assert.AreEqual("Das Zeitintervall für Aktualisierung kann nicht kleiner als 0.5s sein" +
                      " (oder 0 für Aktualisierung nur bei eingehenden Telegrammen).", result.Message);
      // Try to update never
      node.mUpdateTime.Value = new TimeSpan(0);
      result = node.Validate("de");
      Assert.False(result.HasError);

      // Statistics over 0 ==> unlimited time
      node.mConsideredTime.Value = new TimeSpan(0);
      // Try to update at least every 0.49s
      node.mUpdateTime.Value = new TimeSpan(0, 0, 0, 0, 490);
      result = node.Validate("en");
      Assert.True(result.HasError);
      Assert.AreEqual("The update period cannot be less than 0.5s (or 0 for no updates between" +
                      " received telegrams).", result.Message);
      result = node.Validate("de");
      Assert.True(result.HasError);
      Assert.AreEqual("Das Zeitintervall für Aktualisierung kann nicht kleiner als 0.5s sein" +
                      " (oder 0 für Aktualisierung nur bei eingehenden Telegrammen).", result.Message);
      // Try to update never
      node.mUpdateTime.Value = new TimeSpan(0);
      result = node.Validate("en");
      Assert.False(result.HasError);
    }

    [Test]
    public void UnlimitedHistoryTest()
    {
      // Statistics over unlimited time, up to 50 entries stored for
      // averaging
      node.mConsideredTime.Value = new TimeSpan(0);
      // No updates between sent values by default

      // Input a deterministic series of 100 increasing values that
      // makes it easy to check all output values 
      for (int i = 0; i <= 100; i++)
      {
        schedulerService.Tick(/* advance by */ 1 /* second */);
        node.mInput.Value = i;
        node.Execute();
        Assert.AreEqual(Math.Min(i + 1, 51), node.mOutputNumber.Value);
        Assert.AreEqual(i / 2.0, node.mOutputAvg.Value);
        Assert.AreEqual(0, node.mOutputMin.Value);
        Assert.AreEqual(i, node.mOutputMax.Value);
        if (i < 1)        // not enoug samples to get a trend
          Assert.IsFalse(node.mOutputChange.HasValue);
        else
          Assert.AreEqual(i, node.mOutputChange.Value);
        // Because the value increment is below the 2*resolution threshold, a trend
        // can only be determined when new values are 4*resolution above the average
        if (i < 2)        // not enoug samples to get a trend
          Assert.IsFalse(node.mOutputTrend.HasValue);
        else if (i < 3)
          Assert.AreEqual(0, node.mOutputTrend.Value);
        else
          Assert.AreEqual(1, node.mOutputTrend.Value);
      }

      // Continue the series for another 100 values up, knowing that from now
      // on each new entry discards an old one. The average may therefore become
      // slightly less accurate, but schould still increase at the same rate.
      // Also minimum and maximum should not rely on the recorded history, but
      // be directly updated from received values.
      for (int i = 1; i <= 100; i++)
      {
        schedulerService.Tick(/* advance by */ 1 /* second */);
        node.mInput.Value = 100 + i;
        node.Execute();
        Assert.AreEqual(51, node.mOutputNumber.Value);
        Assert.AreEqual(50.0 + i / 2.0, node.mOutputAvg.Value);
        Assert.AreEqual(0, node.mOutputMin.Value);
        Assert.AreEqual(100 + i, node.mOutputMax.Value);
        Assert.AreEqual(100 + i, node.mOutputChange.Value);
        Assert.AreEqual(1, node.mOutputTrend.Value);
      }

      var expectAvg = 100.0;
      Assert.AreEqual(expectAvg, node.mOutputAvg.Value);

      // Continue the series for another 100 values, each 2.0 down from its
      // predecessor
      for (int i = 1; i <= 100; i++)
      {
        // Average starts at 150.0. Each entry added will add (201 - 2 * i) to
        // the integral, 
        schedulerService.Tick(/* advance by */ 1 /* second */);
        node.mInput.Value = 200 - 2 * i;
        node.Execute();
        Assert.AreEqual(51, node.mOutputNumber.Value);
        expectAvg = (expectAvg * (200 + i - 1) + (201.0 - 2 * i)) / (200 + i);
        Assert.AreEqual(expectAvg, node.mOutputAvg.Value, 5e-14);
        Assert.AreEqual(0, node.mOutputMin.Value);
        Assert.AreEqual(200, node.mOutputMax.Value);
        Assert.AreEqual(200 - 2 * i, node.mOutputChange.Value);
        if (i < 2)    // trend about to turn
          Assert.AreEqual(0, node.mOutputTrend.Value);
        else          // trend falling from now on
          Assert.AreEqual(-1, node.mOutputTrend.Value);
      }

      expectAvg = 100.0;
      Assert.AreEqual(expectAvg, node.mOutputAvg.Value, 5e-14);

      // Continue the series for another 100 values, each 4.0 down from its
      // predecessor
      for (int i = 1; i <= 100; i++)
      {
        // Average starts at 100.0. Each entry added will add (2 - 4 * i) to
        // the integral, 
        schedulerService.Tick(/* advance by */ 1 /* second */);
        node.mInput.Value = -4 * i;
        node.Execute();
        Assert.AreEqual(51, node.mOutputNumber.Value);
        expectAvg = (expectAvg * (300 + i - 1) + (2.0 - 4 * i)) / (300 + i);
        Assert.AreEqual(expectAvg, node.mOutputAvg.Value, 8e-14);
        Assert.AreEqual(-4 * i, node.mOutputMin.Value);
        Assert.AreEqual(200, node.mOutputMax.Value);
        Assert.AreEqual(-4 * i, node.mOutputChange.Value);
        Assert.AreEqual(-1, node.mOutputTrend.Value);  // trend continues falling
      }
      Assert.AreEqual(25, node.mOutputAvg.Value, 8e-14);
      Assert.AreEqual(-400, node.mOutputMin.Value);

      // Reset
      node.mReset.Value = true;
      node.mInput.WasSet = false;
      node.Execute();
      Assert.AreEqual(0, node.mOutputNumber.Value);
      Assert.AreEqual(-400, node.mOutputAvg.Value, 8e-14);
      Assert.AreEqual(-400, node.mOutputMin.Value);
      Assert.AreEqual(-400, node.mOutputMax.Value);
      Assert.AreEqual(0, node.mOutputTrend.Value);   // trend flattened
    }

    [Test]
    public void FilterResolutionTest()
    {
      // Statistics over 10s
      node.mConsideredTime.Value = new TimeSpan(0, 0, 10);
      // No updates betwenn sent values by default

      node.mInput.Value = 5;
      node.Execute();
      Assert.AreEqual(1, node.mOutputNumber.Value);
      Assert.AreEqual(5, node.mOutputAvg.Value, 8e-14);
      Assert.AreEqual(5, node.mOutputMin.Value);
      Assert.AreEqual(5, node.mOutputMax.Value);
      Assert.IsFalse(node.mOutputTrend.HasValue);

      schedulerService.Tick(/* advance by */ 1 /* second */);
      node.mInput.Value = 1;
      node.Execute();
      Assert.AreEqual(2, node.mOutputNumber.Value);
      Assert.AreEqual(3, node.mOutputAvg.Value, 8e-14);
      Assert.AreEqual(1, node.mOutputMin.Value);
      Assert.AreEqual(5, node.mOutputMax.Value);
      Assert.IsFalse(node.mOutputTrend.HasValue);

      schedulerService.Tick(/* advance by */ 1 /* second */);
      node.mInput.Value = 1;
      node.Execute();
      Assert.AreEqual(3, node.mOutputNumber.Value);
      Assert.AreEqual((3.0 + 1) / 2, node.mOutputAvg.Value);
      Assert.AreEqual(1, node.mOutputMin.Value);
      Assert.AreEqual(5, node.mOutputMax.Value);
      Assert.AreEqual(-1, node.mOutputTrend.Value);

      schedulerService.Tick(/* advance by */ 1 /* second */);
      node.mInput.Value = 1;
      node.Execute();
      Assert.AreEqual(3, node.mOutputNumber.Value);
      Assert.AreEqual((3.0 + 1 + 1) / 3, node.mOutputAvg.Value);
      Assert.AreEqual(1, node.mOutputMin.Value);
      Assert.AreEqual(5, node.mOutputMax.Value);
      Assert.AreEqual(-1, node.mOutputTrend.Value);

      schedulerService.Tick(/* advance by */ 1 /* second */);
      node.mInput.Value = 9;
      node.Execute();
      Assert.AreEqual(4, node.mOutputNumber.Value);
      Assert.AreEqual((3.0 + 1 + 1 + 5) / 4, node.mOutputAvg.Value);
      Assert.AreEqual(1, node.mOutputMin.Value);
      Assert.AreEqual(9, node.mOutputMax.Value);
      Assert.AreEqual(0, node.mOutputTrend.Value);

      schedulerService.Tick(/* advance by */ 1 /* second */);
      node.mInput.Value = 11;
      node.Execute();
      Assert.AreEqual(5, node.mOutputNumber.Value);
      Assert.AreEqual((3.0 + 1 + 1 + 5 + 10) / 5, node.mOutputAvg.Value);
      Assert.AreEqual(1, node.mOutputMin.Value);
      Assert.AreEqual(11, node.mOutputMax.Value);
      Assert.AreEqual(1, node.mOutputTrend.Value);
    }

    // Issue #1 (Geringfügig falscher Durchschnitts-Ausgabewert,
    // wenn länger als für den Betrachtungszeitraum keine neuen
    // Werte eingegeben werden)
    [Test]
    public void UpdateTest1()
    {
      // Statistics over 10s
      node.mConsideredTime.Value = new TimeSpan(0, 0, 10);
      // Update at least every 3 seconds
      node.mUpdateTime.Value = new TimeSpan(0, 0, 3);

      // At mocked time 0, input a first value
      node.mInput.Value = 0.0;
      node.Execute();
      // Ensure that the min/max/avg values are all just that value
      checkValues();

      // Advance mocked time in 1s steps but don't input a second value for 12s
      for ( int i = 1; i <= 12; i++ )
      {
        schedulerService.Tick(/* advance by */ 1 /* second */);
        // Ensure that the values are all unchanged
        checkValues();
      }

      // At mocked time 12s, input a second value
      node.mInput.Value = 1.0;
      node.Execute();
      // Ensure that the output values change accordingly
      checkValues(2, 0.5, 1.0, 0.0, 1.0);

      // Advance mocked time in 1s steps but don't input a third value,
      // then check how the values change over time
      double[] exp_min = { 0.0,
                           (5.0/12.0),
                           (8.0/12.0),
                           (11/12.0),
                           1.0
                         };
      double[] exp_avg = { 0.5,
                           (8.5/12.0) * 0.7 + 1.0 * 0.3,
                           (10.0/12.0) * 0.4 + 1.0 * 0.6,
                           (11.5/12.0) * 0.1 + 1.0 * 0.9,
                           1.0
                         };
      for (int i = 1; i <= 14; i++)
      {
        schedulerService.Tick(/* advance by */ 1 /* second */);
        // Ensure that the average changes accordingly (other values unchanged)
        checkValues((i >= 12) ? 1 : 2, exp_avg[i/3],
                    1.0,  exp_min[i/3], 1.0 - exp_min[i / 3]);
      }
    }

    private void checkValues(int n = 1,
      double avg = 0.0, double max = 0.0, double min = 0.0,
      Double change = Double.NaN, int trend = -2)
    {
      Assert.IsTrue(node.mOutputNumber.HasValue);
      Assert.AreEqual(n, node.mOutputNumber.Value);

      Assert.IsTrue(node.mOutputAvg.HasValue);
      Assert.AreEqual(avg, node.mOutputAvg.Value, 5e-16);
      Assert.IsTrue(node.mOutputMax.HasValue);
      Assert.AreEqual(max, node.mOutputMax.Value);
      Assert.IsTrue(node.mOutputMin.HasValue);
      Assert.AreEqual(min, node.mOutputMin.Value, 2e-16);

      if ( !Double.IsNaN(change) )
      {
        Assert.AreEqual(change, node.mOutputChange.Value, 2e-16);
      }
      else
      {
        Assert.IsFalse(node.mOutputChange.HasValue);
      }
      if ( trend != -2 )
      {
        Assert.AreEqual(trend, node.mOutputTrend.Value);
      }
      else
      {
        Assert.IsFalse(node.mOutputTrend.HasValue);
      }
    }

  [Test]
    public void UpdateTest2()
    {
      // Statistics over 10s
      node.mConsideredTime.Value = new TimeSpan(0, 0, 10);
      // Update at least every second
      node.mUpdateTime.Value = new TimeSpan(0, 0, 1);

      // Check start values
      Assert.IsFalse(node.mOutputAvg.HasValue);
      Assert.IsFalse(node.mOutputMin.HasValue);
      Assert.IsFalse(node.mOutputMax.HasValue);
      Assert.IsFalse(node.mOutputChange.HasValue);
      Assert.IsFalse(node.mOutputTrend.HasValue);

      // At mocked time 0, input a first value
      node.mInput.Value = -50;
      node.Execute();
      // Ensure that the min/max/avg values are all just that value
      Assert.AreEqual(-50, node.mOutputAvg.Value);
      Assert.AreEqual(-50, node.mOutputMin.Value);
      Assert.AreEqual(-50, node.mOutputMax.Value);
      Assert.AreEqual(1, node.mOutputNumber.Value);
      Assert.IsFalse(node.mOutputChange.HasValue);
      Assert.IsFalse(node.mOutputTrend.HasValue);

      // Advance mocked time to 1s but don't input a second value yet
      schedulerService.Tick(/* advance by */ 1 /* second */);
      // Ensure that the min/max/avg values are all unchanged
      Assert.AreEqual(-50, node.mOutputAvg.Value);
      Assert.AreEqual(-50, node.mOutputMin.Value);
      Assert.AreEqual(-50, node.mOutputMax.Value);
      Assert.AreEqual(1, node.mOutputNumber.Value);
      Assert.IsFalse(node.mOutputChange.HasValue);
      Assert.IsFalse(node.mOutputTrend.HasValue);

      // Advance mocked time to 2s and input a second value
      schedulerService.Tick(/* advance by */ 1 /* second */);
      node.mInput.Value = 50;
      node.Execute();
      // Ensure that the average is centered between the two values
      Assert.AreEqual(0, node.mOutputAvg.Value);
      Assert.AreEqual(-50, node.mOutputMin.Value);
      Assert.AreEqual(50, node.mOutputMax.Value);
      Assert.AreEqual(2, node.mOutputNumber.Value);
      Assert.AreEqual(100, node.mOutputChange.Value);
      Assert.IsFalse(node.mOutputTrend.HasValue);

      // From now on we add no values, but still want to see the average
      // approach the second value as time goes by. No other values should
      // change -- not even the trend.

      // For the next 8s, the first 2s of the considered timespan have an
      // average of 0, while an increasing number of seconds after that
      // has an average of 50, which gains more and more weight.
      schedulerService.Tick(/* advance by */ 1 /* second */);
      Assert.AreEqual(50.0 / 3, node.mOutputAvg.Value);
      Assert.AreEqual(-50, node.mOutputMin.Value);
      Assert.AreEqual(50, node.mOutputMax.Value);
      Assert.AreEqual(2, node.mOutputNumber.Value);
      Assert.AreEqual(100, node.mOutputChange.Value);
      Assert.IsFalse(node.mOutputTrend.HasValue);
      schedulerService.Tick(/* advance by */ 1 /* second */);
      Assert.AreEqual(100 / 4, node.mOutputAvg.Value);
      Assert.AreEqual(-50, node.mOutputMin.Value);
      Assert.AreEqual(50, node.mOutputMax.Value);
      Assert.AreEqual(2, node.mOutputNumber.Value);
      Assert.AreEqual(100, node.mOutputChange.Value);
      Assert.IsFalse(node.mOutputTrend.HasValue);
      schedulerService.Tick(/* advance by */ 1 /* second */);
      Assert.AreEqual(150 / 5, node.mOutputAvg.Value);
      Assert.AreEqual(-50, node.mOutputMin.Value);
      Assert.AreEqual(50, node.mOutputMax.Value);
      Assert.AreEqual(2, node.mOutputNumber.Value);
      Assert.AreEqual(100, node.mOutputChange.Value);
      Assert.IsFalse(node.mOutputTrend.HasValue);
      schedulerService.Tick(/* advance by */ 1 /* second */);
      Assert.AreEqual(200.0 / 6, node.mOutputAvg.Value);
      Assert.AreEqual(-50, node.mOutputMin.Value);
      Assert.AreEqual(50, node.mOutputMax.Value);
      Assert.AreEqual(2, node.mOutputNumber.Value);
      Assert.AreEqual(100, node.mOutputChange.Value);
      Assert.IsFalse(node.mOutputTrend.HasValue);
      schedulerService.Tick(/* advance by */ 1 /* second */);
      Assert.AreEqual(250.0 / 7, node.mOutputAvg.Value);
      Assert.AreEqual(-50, node.mOutputMin.Value);
      Assert.AreEqual(50, node.mOutputMax.Value);
      Assert.AreEqual(2, node.mOutputNumber.Value);
      Assert.AreEqual(100, node.mOutputChange.Value);
      Assert.IsFalse(node.mOutputTrend.HasValue);
      schedulerService.Tick(/* advance by */ 1 /* second */);
      Assert.AreEqual(300.0 / 8, node.mOutputAvg.Value);
      Assert.AreEqual(-50, node.mOutputMin.Value);
      Assert.AreEqual(50, node.mOutputMax.Value);
      Assert.AreEqual(2, node.mOutputNumber.Value);
      Assert.AreEqual(100, node.mOutputChange.Value);
      Assert.IsFalse(node.mOutputTrend.HasValue);
      schedulerService.Tick(/* advance by */ 1 /* second */);
      Assert.AreEqual(350.0 / 9, node.mOutputAvg.Value);
      Assert.AreEqual(-50, node.mOutputMin.Value);
      Assert.AreEqual(50, node.mOutputMax.Value);
      Assert.AreEqual(2, node.mOutputNumber.Value);
      Assert.AreEqual(100, node.mOutputChange.Value);
      Assert.IsFalse(node.mOutputTrend.HasValue);
      schedulerService.Tick(/* advance by */ 1 /* second */);
      Assert.AreEqual(400.0 / 10, node.mOutputAvg.Value);
      Assert.AreEqual(-50, node.mOutputMin.Value);
      Assert.AreEqual(50, node.mOutputMax.Value);
      Assert.AreEqual(2, node.mOutputNumber.Value);
      Assert.AreEqual(100, node.mOutputChange.Value);
      Assert.IsFalse(node.mOutputTrend.HasValue);

      // After 11s, we have moved half of the first 2s out of the
      // considered timespan, so we expect the average of the remaining
      // first second to increase from 0 to 25, while at the same time
      // we add another second of average 50 at the end.
      schedulerService.Tick(/* advance by */ 1 /* second */);
      Assert.AreEqual((25 + 450.0) / 10, node.mOutputAvg.Value);
      Assert.AreEqual(0, node.mOutputMin.Value);
      Assert.AreEqual(50, node.mOutputMax.Value);
      Assert.AreEqual(2, node.mOutputNumber.Value);
      Assert.AreEqual(50, node.mOutputChange.Value);
      Assert.IsFalse(node.mOutputTrend.HasValue);

      // After 12s, we have moved the first value completely out of
      // the considered timespan, so from now on we expect to only
      // see the second value. We stress this a little to test that
      // - the data handling is robust when removing entries until
      //   just one is left
      // - the node continues to work as expected with just one entry
      //   left
      schedulerService.Tick(/* advance by */ 1 /* second */);
      Assert.AreEqual(50, node.mOutputAvg.Value);
      Assert.AreEqual(50, node.mOutputMin.Value);
      Assert.AreEqual(50, node.mOutputMax.Value);
      Assert.AreEqual(1, node.mOutputNumber.Value);
      Assert.AreEqual(0, node.mOutputChange.Value);
      Assert.IsFalse(node.mOutputTrend.HasValue);
      schedulerService.Tick(/* advance by */ 1 /* second */);
      Assert.AreEqual(50, node.mOutputAvg.Value);
      Assert.AreEqual(50, node.mOutputMin.Value);
      Assert.AreEqual(50, node.mOutputMax.Value);
      Assert.AreEqual(1, node.mOutputNumber.Value);
      Assert.AreEqual(0, node.mOutputChange.Value);
      Assert.IsFalse(node.mOutputTrend.HasValue);
      schedulerService.Tick(/* advance by */ 1 /* second */);
      Assert.AreEqual(50, node.mOutputAvg.Value);
      Assert.AreEqual(50, node.mOutputMin.Value);
      Assert.AreEqual(50, node.mOutputMax.Value);
      Assert.AreEqual(1, node.mOutputNumber.Value);
      Assert.AreEqual(0, node.mOutputChange.Value);
      Assert.IsFalse(node.mOutputTrend.HasValue);
      schedulerService.Tick(/* advance by */ 1 /* second */);
      Assert.AreEqual(50, node.mOutputAvg.Value);
      Assert.AreEqual(50, node.mOutputMin.Value);
      Assert.AreEqual(50, node.mOutputMax.Value);
      Assert.AreEqual(1, node.mOutputNumber.Value);
      Assert.AreEqual(0, node.mOutputChange.Value);
      Assert.IsFalse(node.mOutputTrend.HasValue);
      schedulerService.Tick(/* advance by */ 1 /* second */);
      Assert.AreEqual(50, node.mOutputAvg.Value);
      Assert.AreEqual(50, node.mOutputMin.Value);
      Assert.AreEqual(50, node.mOutputMax.Value);
      Assert.AreEqual(1, node.mOutputNumber.Value);
      Assert.AreEqual(0, node.mOutputChange.Value);
      Assert.IsFalse(node.mOutputTrend.HasValue);
      schedulerService.Tick(/* advance by */ 1 /* second */);
      Assert.AreEqual(50, node.mOutputAvg.Value);
      Assert.AreEqual(50, node.mOutputMin.Value);
      Assert.AreEqual(50, node.mOutputMax.Value);
      Assert.AreEqual(1, node.mOutputNumber.Value);
      Assert.AreEqual(0, node.mOutputChange.Value);
      Assert.IsFalse(node.mOutputTrend.HasValue);
      schedulerService.Tick(/* advance by */ 1 /* second */);
      Assert.AreEqual(50, node.mOutputAvg.Value);
      Assert.AreEqual(50, node.mOutputMin.Value);
      Assert.AreEqual(50, node.mOutputMax.Value);
      Assert.AreEqual(1, node.mOutputNumber.Value);
      Assert.AreEqual(0, node.mOutputChange.Value);
      Assert.IsFalse(node.mOutputTrend.HasValue);
    }

    [Test]
    public void ValuesWithUpdateTest()
    {
      // Statistics over 10s
      node.mConsideredTime.Value = new TimeSpan(0, 0, 10);
      // Update at least every second
      node.mUpdateTime.Value = new TimeSpan(0, 0, 1);

      // Check start output values
      Assert.IsFalse(node.mOutputAvg.HasValue);
      Assert.IsFalse(node.mOutputMin.HasValue);
      Assert.IsFalse(node.mOutputMax.HasValue);
      Assert.IsFalse(node.mOutputTrend.HasValue);

      // At mocked time 0, input a first value
      node.mInput.Value = 3;
      node.Execute();
      // Ensure that all value outputs have just that value
      Assert.AreEqual(3, node.mOutputAvg.Value);
      Assert.AreEqual(3, node.mOutputMin.Value);
      Assert.AreEqual(3, node.mOutputMax.Value);
      Assert.IsFalse(node.mOutputTrend.HasValue);  // no trend yet

      // Advance mocked time to 1s but don't input a second value yet
      schedulerService.Tick(/* advance by */ 1 /* second */);
      // Ensure that all outputs are unchanged
      Assert.AreEqual(3, node.mOutputAvg.Value);
      Assert.AreEqual(3, node.mOutputMin.Value);
      Assert.AreEqual(3, node.mOutputMax.Value);
      Assert.IsFalse(node.mOutputTrend.HasValue);  // no trend yet

      // Advance mocked time to 2s and input a second value
      schedulerService.Tick(/* advance by */ 1 /* second */);
      node.mInput.Value = 11;
      node.Execute();
      // Ensure that the average is centered between the two values
      Assert.AreEqual(7.0, node.mOutputAvg.Value);
      Assert.AreEqual(3, node.mOutputMin.Value);
      Assert.AreEqual(11, node.mOutputMax.Value);
      Assert.IsFalse(node.mOutputTrend.HasValue);  // no clear trend yet

      // Advance mocked time to 3s but don't input a third value yet
      schedulerService.Tick(/* advance by */ 1 /* second */);
      // Ensure that the average is updated with constant extrapolation
      Assert.AreEqual((2 * 7.0 + 11.0) / 3, node.mOutputAvg.Value);
      Assert.AreEqual(3, node.mOutputMin.Value);
      Assert.AreEqual(11, node.mOutputMax.Value);
      Assert.IsFalse(node.mOutputTrend.HasValue);  // no clear trend yet

      // Advance mocked time to 7s and input a third value
      schedulerService.Tick(/* advance by */ 4 /* second */);
      node.mInput.Value = 16;
      node.Execute();
      // Ensure that the average is really the weighted average
      // (Non-weighted average would be 10)
      Assert.AreEqual((2 * 7.0 + 5 * 13.5) / 7, node.mOutputAvg.Value);
      Assert.AreEqual(3, node.mOutputMin.Value);
      Assert.AreEqual(16, node.mOutputMax.Value);
      Assert.AreEqual(1, node.mOutputTrend.Value);

      // Advance mocked time to 8s but don't input a fourth value yet
      schedulerService.Tick(/* advance by */ 1 /* seconds */);
      // Ensure that the average is updated with constant extrapolation
      Assert.AreEqual((2 * 7.0 + 5 * 13.5 + 16.0) / 8, node.mOutputAvg.Value);
      Assert.AreEqual(3, node.mOutputMin.Value);
      Assert.AreEqual(16, node.mOutputMax.Value);
      Assert.AreEqual(1, node.mOutputTrend.Value);

      // Advance mocked time to 11s and input a fourth value
      schedulerService.Tick(/* advance by */ 3 /* second */);
      node.mInput.Value = 13;
      node.Execute();
      // Ensure that the average is really the weighted average without
      // taking the first second into account.
      Assert.AreEqual((9.0 + 5 * 13.5 + 4 * 14.5) / 10, node.mOutputAvg.Value);
      // Ensure that the minimum is updated with the correct interpolated value 
      Assert.AreEqual(7.0, node.mOutputMin.Value);
      Assert.AreEqual(16, node.mOutputMax.Value);
      Assert.AreEqual(1, node.mOutputTrend.Value); // no clear trend any more

      // Advance mocked time to 13s but don't input a fifth value
      schedulerService.Tick(/* advance by */ 2 /* seconds */);
      // Ensure that the average is updated with constant extrapolation
      Assert.AreEqual((4 * 14.0 + 4 * 14.5 + 2 * 13.0) / 10, node.mOutputAvg.Value);
      // Ensure that the minimum is updated with the correct interpolated value 
      Assert.AreEqual(12.0, node.mOutputMin.Value);
      Assert.AreEqual(16, node.mOutputMax.Value);
      Assert.AreEqual(0, node.mOutputTrend.Value);

      // Advance mocked time to 16s and input a fifth value
      schedulerService.Tick(/* advance by */ 3 /* second */);
      node.mInput.Value = 11;
      node.Execute();
      Assert.AreEqual((1 * 15.5 + 4 * 14.5 + 5 * 12.0) / 10, node.mOutputAvg.Value);
      // Ensure that the minimum is updated with the correct interpolated value 
      Assert.AreEqual(11.0, node.mOutputMin.Value);
      Assert.AreEqual(16, node.mOutputMax.Value);
      Assert.AreEqual(-1, node.mOutputTrend.Value);
    }

    [Test]
    public void ValuesWithoutUpdateTest()
    {
      // Statistics over 10s
      node.mConsideredTime.Value = new TimeSpan(0, 0, 10);
      // Default is to update only when new values arrive

      // Check start output values
      Assert.IsFalse(node.mOutputAvg.HasValue);
      Assert.IsFalse(node.mOutputMin.HasValue);
      Assert.IsFalse(node.mOutputMax.HasValue);
      Assert.IsFalse(node.mOutputTrend.HasValue);

      // At mocked time 0 input a first value
      node.mInput.Value = 3;
      node.Execute();
      // Ensure all value outputs have value
      Assert.AreEqual(3, node.mOutputAvg.Value);
      Assert.AreEqual(3, node.mOutputMin.Value);
      Assert.AreEqual(3, node.mOutputMax.Value);
      Assert.IsFalse(node.mOutputTrend.HasValue);  // no trend yet

      // Advance mocked time to 1s but don't input a second value yet
      schedulerService.Tick(/* advance by */ 1 /* second */);
      // Ensure that all value outputs are unchanged
      Assert.AreEqual(3, node.mOutputAvg.Value);
      Assert.AreEqual(3, node.mOutputMin.Value);
      Assert.AreEqual(3, node.mOutputMax.Value);
      Assert.IsFalse(node.mOutputTrend.HasValue);  // no trend yet

      // Advance mocked time to 2s and input a second value
      schedulerService.Tick(/* advance by */ 1 /* second */);
      node.mInput.Value = 11;
      node.Execute();
      // Ensure that the average is centered between the two values, and
      // Maximum and Trend are updated accordingly
      Assert.AreEqual(7.0, node.mOutputAvg.Value);
      Assert.AreEqual(3, node.mOutputMin.Value);
      Assert.AreEqual(11, node.mOutputMax.Value);
      Assert.IsFalse(node.mOutputTrend.HasValue);  // no trend yet

      // Advance mocked time to 3s but don't input a third value yet
      schedulerService.Tick(/* advance by */ 1 /* second */);
      // Ensure that all outputs stay unchanged
      Assert.AreEqual(7.0, node.mOutputAvg.Value);
      Assert.AreEqual(3, node.mOutputMin.Value);
      Assert.AreEqual(11, node.mOutputMax.Value);
      Assert.IsFalse(node.mOutputTrend.HasValue);  // no trend yet

      // Advance mocked time to 7s and input a third value
      schedulerService.Tick(/* advance by */ 4 /* second */);
      node.mInput.Value = 16;
      node.Execute();
      // Ensure that the average is really the weighted average
      // (Non-weighted average would be 10), and Maximum is updated
      Assert.AreEqual((2 * 7.0 + 5 * 13.5) / 7, node.mOutputAvg.Value);
      Assert.AreEqual(3, node.mOutputMin.Value);
      Assert.AreEqual(16, node.mOutputMax.Value);
      Assert.AreEqual(1, node.mOutputTrend.Value);

      // Advance mocked time to 8s but don't input a fourth value yet
      schedulerService.Tick(/* advance by */ 1 /* seconds */);
      // Ensure that all outputs stay unchanged
      Assert.AreEqual((2 * 7.0 + 5 * 13.5) / 7, node.mOutputAvg.Value);
      Assert.AreEqual(3, node.mOutputMin.Value);
      Assert.AreEqual(16, node.mOutputMax.Value);
      Assert.AreEqual(1, node.mOutputTrend.Value);

      // Advance mocked time to 11s and input a fourth value
      schedulerService.Tick(/* advance by */ 3 /* second */);
      node.mInput.Value = 13;
      node.Execute();
      // Ensure that the average is really the weighted average without
      // taking the first second into account, and Trend is updated
      Assert.AreEqual((9.0 + 5 * 13.5 + 4 * 14.5) / 10, node.mOutputAvg.Value);
      // Ensure that the minimum is updated with the correct interpolated value 
      Assert.AreEqual(7.0, node.mOutputMin.Value);
      Assert.AreEqual(16, node.mOutputMax.Value);
      Assert.AreEqual(1, node.mOutputTrend.Value); // no clear trend any more

      // Advance mocked time to 13s but don't input a fifth value
      schedulerService.Tick(/* advance by */ 2 /* seconds */);
      // Ensure that all outputs stay unchanged
      Assert.AreEqual((9.0 + 5 * 13.5 + 4 * 14.5) / 10, node.mOutputAvg.Value);
      Assert.AreEqual(7.0, node.mOutputMin.Value);
      Assert.AreEqual(16, node.mOutputMax.Value);
      Assert.AreEqual(1, node.mOutputTrend.Value);

      // Advance mocked time to 16s and input a fifth value
      schedulerService.Tick(/* advance by */ 3 /* second */);
      node.mInput.Value = 11;
      node.Execute();
      Assert.AreEqual((1 * 15.5 + 4 * 14.5 + 5 * 12.0) / 10, node.mOutputAvg.Value);
      // Ensure that the minimum is updated with the correct interpolated value 
      Assert.AreEqual(11.0, node.mOutputMin.Value);
      Assert.AreEqual(16, node.mOutputMax.Value);
      Assert.AreEqual(-1, node.mOutputTrend.Value);
    }

    [Test]
    public void ResolutionDefaultTest()
    {
      // Statistics over 10s
      node.mConsideredTime.Value = new TimeSpan(0, 0, 10);
      // Resolution default is 1.0

      // Check start output values
      Assert.IsFalse(node.mOutputAvg.HasValue);
      Assert.IsFalse(node.mOutputMin.HasValue);
      Assert.IsFalse(node.mOutputMax.HasValue);
      Assert.IsFalse(node.mOutputTrend.HasValue);

      // At mocked time 0 input a first value
      node.mInput.Value = 3.0;
      node.Execute();
      Assert.AreEqual(3.0, node.mOutputAvg.Value);
      Assert.AreEqual(3.0, node.mOutputMin.Value);
      Assert.AreEqual(3.0, node.mOutputMax.Value);
      Assert.IsFalse(node.mOutputTrend.HasValue);

      schedulerService.Tick(/* advance by */ 2 /* seconds */);
      node.mInput.Value = 2.0000001;
      node.Execute();
      Assert.AreEqual((3.0 + 2.0000001) / 2, node.mOutputAvg.Value);
      Assert.AreEqual(2.0000001, node.mOutputMin.Value);
      Assert.AreEqual(3.0, node.mOutputMax.Value);
      Assert.IsFalse(node.mOutputTrend.HasValue);

      schedulerService.Tick(/* advance by */ 2 /* seconds */);
      node.mInput.Value = 3.0;
      node.Execute();
      Assert.AreEqual(3.0, node.mOutputAvg.Value);
      Assert.AreEqual(3.0, node.mOutputMin.Value);
      Assert.AreEqual(3.0, node.mOutputMax.Value);
      Assert.IsFalse(node.mOutputTrend.HasValue);

      schedulerService.Tick(/* advance by */ 2 /* second */);
      node.mInput.Value = 4.0;
      node.Execute();
      Assert.AreEqual((4 * 3 + 2 * 3.5) / 6, node.mOutputAvg.Value);
      Assert.AreEqual(3.0, node.mOutputMin.Value);
      Assert.AreEqual(4.0, node.mOutputMax.Value);
      Assert.AreEqual(0, node.mOutputTrend.Value);   // no clear trend yet

      schedulerService.Tick(/* advance by */ 1 /* second */);
      node.mInput.Value = 5.002;
      node.Execute();
      Assert.AreEqual((4 * 3 + 2 * 3.5 + 4.501) / 7, node.mOutputAvg.Value, 1e-15);
      Assert.AreEqual(3.0, node.mOutputMin.Value);
      Assert.AreEqual(5.002, node.mOutputMax.Value);
      Assert.AreEqual(0, node.mOutputTrend.Value);   // no clear trend yet

      schedulerService.Tick(/* advance by */ 1 /* second */);
      node.mInput.Value = 5.998;
      node.Execute();
      Assert.AreEqual((4 * 3 + 2 * 3.5 + 4.501 + 5.5) / 8, node.mOutputAvg.Value);
      Assert.AreEqual(3.0, node.mOutputMin.Value);
      Assert.AreEqual(5.998, node.mOutputMax.Value);
      Assert.AreEqual(0, node.mOutputTrend.Value);

      schedulerService.Tick(/* advance by */ 1 /* second */);
      node.mInput.Value = 6.0;
      node.Execute();
      Assert.AreEqual((4 * 3 + 2 * 3.5 + 4.501 + 2 * 5.501) / 9, node.mOutputAvg.Value);
      Assert.AreEqual(3.0, node.mOutputMin.Value);
      Assert.AreEqual(6.0, node.mOutputMax.Value);
      Assert.AreEqual(1, node.mOutputTrend.Value);

      schedulerService.Tick(/* advance by */ 1 /* second */);
      node.mInput.Value = 5.0;
      node.Execute();
      Assert.AreEqual((4 * 3 + 2 * 3.5 + 4.501 + 2 * 5.501 + 5.5) / 10, node.mOutputAvg.Value);
      Assert.AreEqual(3.0, node.mOutputMin.Value);
      Assert.AreEqual(6.0, node.mOutputMax.Value);
      Assert.AreEqual(1, node.mOutputTrend.Value);

      schedulerService.Tick(/* advance by */ 1 /* second */);
      node.mInput.Value = 4;
      node.Execute();
      Assert.AreEqual((3 * 3 + 2 * 3.5 + 4.501 + 2 * 5.501 + 5.5 + 4.5) / 10, node.mOutputAvg.Value);
      Assert.AreEqual(3.0, node.mOutputMin.Value);
      Assert.AreEqual(6.0, node.mOutputMax.Value);
      Assert.AreEqual(1, node.mOutputTrend.Value);

      schedulerService.Tick(/* advance by */ 1 /* second */);
      node.mInput.Value = 2.5;
      node.Execute();
      Assert.AreEqual((2 * 3 + 2 * 3.5 + 4.501 + 2 * 5.501 + 5.5 + 4.5 + 3.25) / 10, node.mOutputAvg.Value);
      Assert.AreEqual(2.5, node.mOutputMin.Value);
      Assert.AreEqual(6.0, node.mOutputMax.Value);
      Assert.AreEqual(0, node.mOutputTrend.Value);

      schedulerService.Tick(/* advance by */ 1 /* second */);
      node.mInput.Value = 1;
      node.Execute();
      Assert.AreEqual((1 * 3 + 2 * 3.5 + 4.501 + 2 * 5.501 + 5.5 + 4.5 + 3.25 + 1.75) / 10, node.mOutputAvg.Value);
      Assert.AreEqual(1.0, node.mOutputMin.Value);
      Assert.AreEqual(6.0, node.mOutputMax.Value);
      Assert.AreEqual(-1, node.mOutputTrend.Value);
    }

    [Test]
    public void ResolutionExplicitTest()
    {
      // Statistics over 10s
      node.mConsideredTime.Value = new TimeSpan(0, 0, 10);
      node.mInputResolution.Value = 0.1;

      Assert.IsFalse(node.mOutputAvg.HasValue);
      Assert.IsFalse(node.mOutputMin.HasValue);
      Assert.IsFalse(node.mOutputMax.HasValue);
      Assert.IsFalse(node.mOutputTrend.HasValue);

      // At mocked time 0 input a first value
      node.mInput.Value = 0.30;
      node.Execute();
      Assert.AreEqual(0.30, node.mOutputAvg.Value);
      Assert.AreEqual(0.30, node.mOutputMin.Value);
      Assert.AreEqual(0.30, node.mOutputMax.Value);
      Assert.IsFalse(node.mOutputTrend.HasValue);

      schedulerService.Tick(/* advance by */ 2 /* seconds */);
      node.mInput.Value = 0.3999999995;
      node.Execute();
      Assert.AreEqual((0.30 + 0.3999999995) / 2, node.mOutputAvg.Value);
      Assert.AreEqual(0.30, node.mOutputMin.Value);
      Assert.AreEqual(0.3999999995, node.mOutputMax.Value);
      Assert.IsFalse(node.mOutputTrend.HasValue);

      schedulerService.Tick(/* advance by */ 2 /* seconds */);
      node.mInput.Value = 0.30;
      node.Execute();
      Assert.AreEqual(0.30, node.mOutputAvg.Value);
      Assert.AreEqual(0.30, node.mOutputMin.Value);
      Assert.AreEqual(0.30, node.mOutputMax.Value);
      Assert.IsFalse(node.mOutputTrend.HasValue);

      schedulerService.Tick(/* advance by */ 2 /* second */);
      node.mInput.Value = 0.2;
      node.Execute();
      Assert.AreEqual((4 * 0.3 + 2 * 0.25) / 6, node.mOutputAvg.Value);
      Assert.AreEqual(0.2, node.mOutputMin.Value);
      Assert.AreEqual(0.30, node.mOutputMax.Value);
      Assert.AreEqual(0, node.mOutputTrend.Value);   // no clear trend yet

      schedulerService.Tick(/* advance by */ 1 /* second */);
      node.mInput.Value = 0.0998;
      node.Execute();
      Assert.AreEqual((4 * 0.3 + 2 * 0.25 + 0.1499) / 7, node.mOutputAvg.Value);
      Assert.AreEqual(0.0998, node.mOutputMin.Value);
      Assert.AreEqual(0.30, node.mOutputMax.Value);
      Assert.AreEqual(0, node.mOutputTrend.Value);   // no clear trend yet

      schedulerService.Tick(/* advance by */ 1 /* second */);
      node.mInput.Value = 0.0002;
      node.Execute();
      Assert.AreEqual((4 * 0.3 + 2 * 0.25 + 0.1499 + 0.05) / 8, node.mOutputAvg.Value);
      Assert.AreEqual(0.0002, node.mOutputMin.Value);
      Assert.AreEqual(0.30, node.mOutputMax.Value);
      Assert.AreEqual(0, node.mOutputTrend.Value);

      schedulerService.Tick(/* advance by */ 1 /* second */);
      node.mInput.Value = 0.0;
      node.Execute();
      Assert.AreEqual((4 * 0.3 + 2 * 0.25 + 0.1499 + 2 * 0.0499) / 9, node.mOutputAvg.Value);
      Assert.AreEqual(0.0, node.mOutputMin.Value);
      Assert.AreEqual(0.30, node.mOutputMax.Value);
      Assert.AreEqual(-1, node.mOutputTrend.Value);

      schedulerService.Tick(/* advance by */ 1 /* second */);
      node.mInput.Value = 0.10;
      node.Execute();
      Assert.AreEqual((4 * 0.3 + 2 * 0.25 + 0.1499 + 2 * 0.0499 + 0.05) / 10, node.mOutputAvg.Value);
      Assert.AreEqual(0.0, node.mOutputMin.Value);
      Assert.AreEqual(0.30, node.mOutputMax.Value);
      Assert.AreEqual(-1, node.mOutputTrend.Value);

      schedulerService.Tick(/* advance by */ 1 /* second */);
      node.mInput.Value = 0.20;
      node.Execute();
      Assert.AreEqual((3 * 0.3 + 2 * 0.25 + 0.1499 + 2 * 0.0499 + 0.05 + 0.15) / 10, node.mOutputAvg.Value, 1e-15);
      Assert.AreEqual(0.0, node.mOutputMin.Value);
      Assert.AreEqual(0.30, node.mOutputMax.Value);
      Assert.AreEqual(-1, node.mOutputTrend.Value);

      schedulerService.Tick(/* advance by */ 1 /* second */);
      node.mInput.Value = 0.35;
      node.Execute();
      Assert.AreEqual((2 * 0.3 + 2 * 0.25 + 0.1499 + 2 * 0.0499 + 0.05 + 0.15 + 0.275) / 10, node.mOutputAvg.Value);
      Assert.AreEqual(0.0, node.mOutputMin.Value);
      Assert.AreEqual(0.35, node.mOutputMax.Value);
      Assert.AreEqual(0, node.mOutputTrend.Value);

      schedulerService.Tick(/* advance by */ 1 /* second */);
      node.mInput.Value = 0.5;
      node.Execute();
      Assert.AreEqual((1 * 0.3 + 2 * 0.25 + 0.1499 + 2 * 0.0499 + 0.05 + 0.15 + 0.275 + 0.425) / 10, node.mOutputAvg.Value, 1e-15);
      Assert.AreEqual(0.0, node.mOutputMin.Value);
      Assert.AreEqual(0.5, node.mOutputMax.Value);
      Assert.AreEqual(1, node.mOutputTrend.Value);
    }

    [Test]
    public void ResetNodeTest()
    {
      // Statistics over 10s
      node.mConsideredTime.Value = new TimeSpan(0, 0, 10);

      // Check start output values
      Assert.IsFalse(node.mOutputAvg.HasValue);
      Assert.IsFalse(node.mOutputMin.HasValue);
      Assert.IsFalse(node.mOutputMax.HasValue);
      Assert.IsFalse(node.mOutputChange.HasValue);
      Assert.IsFalse(node.mOutputTrend.HasValue);

      // At mocked time 0 input a first value
      node.mInput.Value = 10;
      node.Execute();
      Assert.AreEqual(10, node.mOutputAvg.Value);
      Assert.AreEqual(10, node.mOutputMin.Value);
      Assert.AreEqual(10, node.mOutputMax.Value);
      Assert.IsFalse(node.mOutputChange.HasValue);
      Assert.IsFalse(node.mOutputTrend.HasValue);

      schedulerService.Tick(/* advance by */ 1 /* seconds */);
      node.mInput.Value = 20;
      node.Execute();
      Assert.AreEqual(15, node.mOutputAvg.Value);
      Assert.AreEqual(10, node.mOutputMin.Value);
      Assert.AreEqual(20, node.mOutputMax.Value);
      Assert.AreEqual(10, node.mOutputChange.Value);
      Assert.IsFalse(node.mOutputTrend.HasValue);

      schedulerService.Tick(/* advance by */ 1 /* seconds */);
      // No new value, no execute, no update
      Assert.AreEqual(15, node.mOutputAvg.Value);
      Assert.AreEqual(10, node.mOutputMin.Value);
      Assert.AreEqual(20, node.mOutputMax.Value);
      Assert.AreEqual(10, node.mOutputChange.Value);
      Assert.IsFalse(node.mOutputTrend.HasValue);

      node.mReset.Value = true;    // Reset ...
      node.mInput.WasSet = false;  // ... without new value ...
      node.Execute();             // ... uses last value as interim value
      Assert.AreEqual(20, node.mOutputAvg.Value);
      Assert.AreEqual(20, node.mOutputMin.Value);
      Assert.AreEqual(20, node.mOutputMax.Value);
      Assert.AreEqual(0, node.mOutputChange.Value);
      Assert.IsFalse(node.mOutputTrend.HasValue);

      schedulerService.Tick(/* advance by */ 2 /* seconds */);
      // No new value, no execute, no update
      Assert.AreEqual(20, node.mOutputAvg.Value);
      Assert.AreEqual(20, node.mOutputMin.Value);
      Assert.AreEqual(20, node.mOutputMax.Value);
      Assert.AreEqual(0, node.mOutputChange.Value);
      Assert.IsFalse(node.mOutputTrend.HasValue);

      node.mReset.WasSet = false;  // First new value after reset 
      node.mInput.Value = 3;       // replaces interim value
      node.Execute();
      Assert.AreEqual(3, node.mOutputAvg.Value);
      Assert.AreEqual(3, node.mOutputMin.Value);
      Assert.AreEqual(3, node.mOutputMax.Value);
      Assert.AreEqual(0, node.mOutputChange.Value);
      Assert.IsFalse(node.mOutputTrend.HasValue);

      schedulerService.Tick(/* advance by */ 2 /* second */);
      // No new value, no execute, no update
      Assert.AreEqual(3, node.mOutputAvg.Value);
      Assert.AreEqual(3, node.mOutputMin.Value);
      Assert.AreEqual(3, node.mOutputMax.Value);
      Assert.AreEqual(0, node.mOutputChange.Value);
      Assert.IsFalse(node.mOutputTrend.HasValue);

      schedulerService.Tick(/* advance by */ 2 /* second */);
      node.mReset.Value = true;    // Reset with new value
      node.mInput.Value = 7;       // needs no interim value
      node.Execute();
      Assert.AreEqual(7, node.mOutputAvg.Value);
      Assert.AreEqual(7, node.mOutputMin.Value);
      Assert.AreEqual(7, node.mOutputMax.Value);
      Assert.AreEqual(0, node.mOutputChange.Value);
      Assert.IsFalse(node.mOutputTrend.HasValue);

      schedulerService.Tick(/* advance by */ 2 /* second */);
      // No new value, no execute, no update
      Assert.AreEqual(7, node.mOutputAvg.Value);
      Assert.AreEqual(7, node.mOutputMin.Value);
      Assert.AreEqual(7, node.mOutputMax.Value);
      Assert.AreEqual(0, node.mOutputChange.Value);
      Assert.IsFalse(node.mOutputTrend.HasValue);

      // Two more new values to get a rising trend
      schedulerService.Tick(/* advance by */ 2 /* second */);
      node.mReset.Value = false; // Explicitely no reset
      node.mInput.Value = 10;
      node.Execute();
      Assert.AreEqual(8.5, node.mOutputAvg.Value);
      Assert.AreEqual(7, node.mOutputMin.Value);
      Assert.AreEqual(10, node.mOutputMax.Value);
      Assert.AreEqual(3, node.mOutputChange.Value);
      Assert.IsFalse(node.mOutputTrend.HasValue);
      schedulerService.Tick(/* advance by */ 2 /* second */);
      node.mReset.Value = false; // Explicitely no reset
      node.mInput.Value = 12;
      node.Execute();
      Assert.AreEqual((2 * 8.5 + 11.0) / 3, node.mOutputAvg.Value);
      Assert.AreEqual(7, node.mOutputMin.Value);
      Assert.AreEqual(12, node.mOutputMax.Value);
      Assert.AreEqual(5, node.mOutputChange.Value);
      Assert.AreEqual(1, node.mOutputTrend.Value);
      // Reset must flatten the trend
      schedulerService.Tick(/* advance by */ 2 /* second */);
      node.mReset.Value = true;
      node.mInput.WasSet = false;  // No new value
      node.Execute();
      Assert.AreEqual(12, node.mOutputAvg.Value);
      Assert.AreEqual(12, node.mOutputMin.Value);
      Assert.AreEqual(12, node.mOutputMax.Value);
      Assert.AreEqual(0, node.mOutputChange.Value);
      Assert.AreEqual(0, node.mOutputTrend.Value);

      // Three values to get a falling trend
      schedulerService.Tick(/* advance by */ 2 /* second */);
      node.mReset.Value = false; // Explicitely no reset
      node.mInput.Value = 11;
      node.Execute();
      Assert.AreEqual(11, node.mOutputAvg.Value);
      Assert.AreEqual(11, node.mOutputMin.Value);
      Assert.AreEqual(11, node.mOutputMax.Value);
      Assert.AreEqual(0, node.mOutputChange.Value);
      Assert.AreEqual(0, node.mOutputTrend.Value);
      schedulerService.Tick(/* advance by */ 2 /* second */);
      node.mReset.Value = false; // Explicitely no reset
      node.mInput.Value = 7;
      node.Execute();
      Assert.AreEqual(9, node.mOutputAvg.Value);
      Assert.AreEqual(7, node.mOutputMin.Value);
      Assert.AreEqual(11, node.mOutputMax.Value);
      Assert.AreEqual(-4, node.mOutputChange.Value);
      Assert.AreEqual(0, node.mOutputTrend.Value);
      schedulerService.Tick(/* advance by */ 2 /* second */);
      node.mReset.Value = false; // Explicitely no reset
      node.mInput.Value = 5;
      node.Execute();
      Assert.AreEqual((9 + 6) / 2.0, node.mOutputAvg.Value);
      Assert.AreEqual(5, node.mOutputMin.Value);
      Assert.AreEqual(11, node.mOutputMax.Value);
      Assert.AreEqual(-6, node.mOutputChange.Value);
      Assert.AreEqual(-1, node.mOutputTrend.Value);
      // Reset must flatten change and trend, ...
      schedulerService.Tick(/* advance by */ 2 /* second */);
      node.mReset.Value = true;
      node.mInput.Value = 3;   // ... even if a new value is given
      node.Execute();
      Assert.AreEqual(3, node.mOutputAvg.Value);
      Assert.AreEqual(3, node.mOutputMin.Value);
      Assert.AreEqual(3, node.mOutputMax.Value);
      Assert.AreEqual(0, node.mOutputChange.Value);
      Assert.AreEqual(0, node.mOutputTrend.Value);
    }

    [Test]
    public void ExecuteTriggerConcurrencyTest()
    {
      node.mConsideredTime.Value = TimeSpan.FromSeconds(1);
      // such that Trigger/Execute obsolete some values during the test 
      Task[] tasks = { new Task(ExecuteTimed), new Task(TriggerTimed) };
      tasks[0].Start();
      tasks[1].Start();    // execute in parallel
      Task.WaitAll(tasks); // wait until both are finished
    }

    private readonly TimeSpan testTime = TimeSpan.FromSeconds(2);

    private void ExecuteTimed()
    {
      Stopwatch s = new Stopwatch();
      s.Start();
      long loopCount = 0;
      while (s.Elapsed < testTime)
      {
        node.mInput.WasSet = false;
        node.mReset.WasSet = false;

        long elapsed100ms = s.ElapsedMilliseconds/100 + 1;

        if ( loopCount % (100/elapsed100ms) != 0 )
        {
          // Usually provide a new input value
          node.mInput.Value = s.Elapsed.Milliseconds;
        }
        else
        {
          // Increasingly frequently reset the node instead
          node.mReset.Value = true;
        }
        node.Execute();
        loopCount++;
      }
      s.Stop();
    }

    private void TriggerTimed()
    {
      Stopwatch s = new Stopwatch();
      s.Start();
      while (s.Elapsed < testTime)
      {
        // Call Trigger as if an update interval had passed
        node.Trigger();
        schedulerService.Tick(1);
      }
      s.Stop();
    }
  }

}
