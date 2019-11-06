using System;
using System.Collections.Generic;
using System.Linq;
using LogicModule.Nodes.Helpers;
using LogicModule.ObjectModel;
using LogicModule.ObjectModel.TypeSystem;

namespace Recomedia_de.Logic.Generic
{
  /// <summary>
  /// Calculates some statistical values of received input values over a given
  /// timespan (some also allow unlimited time).
  /// </summary>
  /// <remarks>
  /// Unlike the algorithm Gira provides with their Logic Node SDK "TimedAverage"
  /// example, this version
  /// - calculates minimum/maximum and a change value of input data over the whole
  ///   timespan, in addition to average,
  /// - provides a tendency indicator for the most recent changes,
  /// - calculates a weighted average, using linear interpolation between received
  ///   samples (and constant extrapolation from the last sample to the end of the
  ///   timespan, if needed),
  /// - allows to specify an input resolution, in order to filter input data that
  ///   has no significant difference.
  /// - stores only up to ~50 samples, but resamples as needed to fulfil the given
  ///   timesspan even if more values are received.
  /// </remarks>
  public class TimedStatistics : LogicNodeBase
  {
    /// <summary>
    /// Used to handle limited accuracy of doubles.
    /// </summary>
    private const double EPSILON = 1e-15;

    /// <summary>
    /// The scheduler service to access the current time and perform actions in the
    /// future.
    /// </summary>
    private readonly ISchedulerService mSchedulerService;

    /// <summary>
    /// The time when to next update the list & output automatically, if enabled.
    /// </summary>
    private SchedulerToken mUpdateToken;

    /// <summary>
    /// The time-stamped values over the specified time.
    /// </summary>
    private struct TimedValue
    {
      public TimedValue(long time, double interValue, double endValue)
      {
        mEndTime = time;
        mInterValue = interValue;
        mEndValue = endValue;
      }
      public long mEndTime;
      public double mInterValue;
      public double mEndValue;
    };
    private LinkedList<TimedValue> mTimedValues;

    /// <summary>
    /// Initializes a new instance of the <see cref="TimedStatistics"/> class.
    /// </summary>
    /// <param name="context">The node context.</param>
    public TimedStatistics(INodeContext context)
    : base(context)
    {
      context.ThrowIfNull("context");
      mSchedulerService = context.GetService<ISchedulerService>();
      var typeService = context.GetService<ITypeService>();

      // Initialize inputs
      mReset = typeService.CreateBool(PortTypes.Binary, "Reset", false);
      mInput = typeService.CreateDouble(PortTypes.Number, "Input");

      // Initialize Outputs
      mOutputAvg = typeService.CreateDouble(PortTypes.Number, "Avg");
      mOutputMin = typeService.CreateDouble(PortTypes.Number, "Min");
      mOutputMax = typeService.CreateDouble(PortTypes.Number, "Max");
      mOutputChange = typeService.CreateDouble(PortTypes.Number, "Change");
      mOutputTrend = typeService.CreateInt(PortTypes.Integer, "Trend");
      mOutputNumber = typeService.CreateInt(PortTypes.Integer, "Number");

      // Default resolution of input values is 1.0
      mInputResolution = typeService.CreateDouble(PortTypes.Number,
                                                  "InputResolution", 1.0);

      // Default considered timespan is one minute
      mConsideredTime = typeService.CreateTimeSpan(PortTypes.TimeSpan,
                            "ConsideredTime",  new TimeSpan(0, 0, 1, 0));
      // Considered timespan range is 0s (unlimited), or 5s .. 1 year
      mConsideredTime.MinValue = new TimeSpan(0 /* ticks */);
      mConsideredTime.MaxValue = new TimeSpan(366 /* days */, 0, 0, 0);

      // Default update timespan is 0 (updates only upon receiving new values)
      mUpdateTime = typeService.CreateTimeSpan(PortTypes.TimeSpan, "UpdateTime",
                                               new TimeSpan(0, 0, 0, 0));
      // Update timespan range is 0s (never), or 0,5s .. 1 year
      mUpdateTime.MinValue = new TimeSpan(0 /* ticks */);
      mUpdateTime.MaxValue = new TimeSpan(366 /* days */, 0, 0, 0);

      // Default maximum number of stored entries is 50; range is 10 .. 2000
      mMaxEntries = typeService.CreateInt(PortTypes.Integer, "MaxNumber", 50);
      mMaxEntries.MinValue =   10;
      mMaxEntries.MaxValue = 2000;

      mTimedValues = new LinkedList<TimedValue>();
    }

    ~TimedStatistics()
    {
      unschedule();
    }

    /// <summary>
    /// Input for values to calculate the statistical values over timespan of.
    /// </summary>
    [Input(DisplayOrder = 1, IsRequired = true)] // Users cannot disable this input
    public DoubleValueObject mInput { get; private set; }

    /// <summary>
    /// Input to clear the list of values and restart with the first value received.
    /// </summary>
    [Input(DisplayOrder = 2, IsDefaultShown = true, IsInput = true)]
    // not shown and not enabled by default
    public BoolValueObject mReset { get; private set; }

    /// <summary>
    /// Value span to treat "about equal" when adding new values, or looking at
    /// differences in trend calculation.
    /// </summary>
    [Parameter(DisplayOrder = 9, IsDefaultShown = false)]
    public DoubleValueObject mInputResolution { get; private set; }

    /// <summary>
    /// Timespan to consider when calculating output values.
    /// </summary>
    [Parameter(DisplayOrder = 10, IsDefaultShown = true)]
    public TimeSpanValueObject mConsideredTime { get; private set; }

    /// <summary>
    /// Regularly update the output after this time, i. e. discard outdated values
    /// and extrapolate from the last received value even when no new values are
    /// received. <=0 means never.
    /// </summary>
    [Parameter(DisplayOrder = 12, IsDefaultShown = false)]
    public TimeSpanValueObject mUpdateTime { get; private set; }

    /// <summary>
    /// Maximum number of values to store.
    /// </summary>
    [Parameter(DisplayOrder = 14, IsDefaultShown = false)]
    public IntValueObject mMaxEntries { get; private set; }

    /// <summary>
    /// The calculated average value of input values within timespan, if selected.
    /// </summary>
    [Output(DisplayOrder = 1, IsDefaultShown = true)]
    public DoubleValueObject mOutputAvg { get; private set; }

    /// <summary>
    /// The calculated minimum and maximum values of input values within timespan,
    /// if selected.
    /// </summary>
    [Output(DisplayOrder = 1, IsDefaultShown = true)]
    public DoubleValueObject mOutputMin { get; private set; }
    [Output(DisplayOrder = 2, IsDefaultShown = true)]
    public DoubleValueObject mOutputMax { get; private set; }

    /// <summary>
    /// The calculated change of input values within timespan, if selected.
    /// </summary>
    [Output(DisplayOrder = 3, IsDefaultShown = true)]
    public DoubleValueObject mOutputChange { get; private set; }

    /// <summary>
    /// The calculated trend value (falling: -1; steady or instable: 0; rising: 1)
    /// of recent input values, if selected.
    /// </summary>
    [Output(DisplayOrder = 4, IsDefaultShown = true)]
    public IntValueObject mOutputTrend { get; private set; }

    /// <summary>
    /// The number of received values within timespan, if selected.
    /// </summary>
    [Output(DisplayOrder = 5, IsDefaultShown = true)]
    public IntValueObject mOutputNumber { get; private set; }

    // Some time utility methods
    private bool isUnlimitedTimeConsidered()
    {
      long ticks = mConsideredTime.Value.Ticks;
      return (0 == ticks);
    }

    private long getConsideredTimeTicks()
    {
      long ticks = mConsideredTime.Value.Ticks;
      if (0 == ticks)
      {
        ticks = long.MaxValue;
      }
      return ticks;
    }

    private long getBeginTimeTicks(long endTime)
    {
      long ticks = endTime - getConsideredTimeTicks();
      ticks = Math.Max(0, ticks);
      return ticks;
    }

    /// <summary>
    /// Called when the logic sheets are checked for correctness, in order to check
    /// this node's correct configuration.
    /// </summary>
    /// <param name="language">
    /// The language key which is used for localizing the validation message.
    /// </param>
    /// 
    public override ValidationResult Validate(string language)
    {
      var consideredTicks = getConsideredTimeTicks();
      if (consideredTicks != 0)
      {
        if (consideredTicks < (5 * TimeSpan.TicksPerSecond))
        {
          return new ValidationResult
          {
            HasError = true,
            Message = Localize(language, "ConsideredTimeTooSmall")
          };
        }
      }
      var updateTicks = mUpdateTime.Value.Ticks;
      if (updateTicks != 0)
      {
        if (updateTicks < (TimeSpan.TicksPerSecond / 2))
        {
          return new ValidationResult
          {
            HasError = true,
            Message = Localize(language, "UpdateTimeTooSmall")
          };
        }
        if (updateTicks > (consideredTicks / 2))
        {
          return new ValidationResult
          {
            HasError = true,
            Message = Localize(language, "UpdateTimeTooLarge")
          };
        }
      }
      return base.Validate(language);
    }

    /// <summary>
    /// Called when all inputs have values, and any of them receives a new value.
    /// </summary>
    public override void Execute()
    {
      lock(mTimedValues)  // prevent Execute and Trigger from concurrently executing
      {
        // Upon receiving Reset = true, clear the list
        if (mReset.WasSet && mReset.Value)
        {
          unschedule();

          setOutputsFromLastValue();
          mTimedValues.Clear();
          mOutputNumber.Value = 0;

          // not necessary to reschedule until new value is received
        }

        // Upon receiving a new input value, remove/trim outdated entries from the list,
        // add a new entry to its end, and recalculate the selected function(s) from the
        // updated list.
        if (mInput.WasSet)
        {
          unschedule();

          var endTime = trimBeginning();
          appendIfValid(endTime, mInput.Value);
          updateOutputs(endTime, mInput.Value);
          mOutputNumber.Value = mTimedValues.Count();

          reschedule();
        }
      }
    }

    /// <summary>
    /// Called when the update time is elapsed. Public only for testability.
    /// </summary>
    public void Trigger()
    {
      lock(mTimedValues)  // prevent Execute and Trigger from concurrently executing
      {
        unschedule();

        var endTime = trimBeginning();
        updateOutputs(endTime);
        mOutputNumber.Value = mTimedValues.Count();

        reschedule();
      }
    }

    /// <summary>
    /// Remove entries from the beginning of the list that are too old to be considered.
    /// Trim first remaining entry's end time such that the time length for the second
    /// entry is not before the start time of the considered timespan.
    /// </summary>
    private long trimBeginning()
    {
      var returnTime = mSchedulerService.Now.Ticks;
      var beginTime = getBeginTimeTicks(returnTime);
      var numberOfTimedValues = mTimedValues.Count();

      // Remove entries from the beginning of the list that are completely outdated;
      // i. e. we will need neither their timestamp nor their values any more. In
      // other words, we must keep the last entry before the new begin time of the
      // considered timespan, if such an entry exists, and we must keep at least two
      // entries if two or more currently exist.
      while ((numberOfTimedValues > 2) &&
              (mTimedValues.ElementAt(1).mEndTime < beginTime))
      {
        mTimedValues.RemoveFirst();
        numberOfTimedValues--;
      }

      if (numberOfTimedValues > 1)
      {
        // If the first remaining entry ends before the new beginTime, then its
        // values are no longer used, but its end time marks the begin time for
        // the second entry. Therefore
        // - set its end time to the new beginTime,
        // - modify the second remaining entry's value to represent the linear
        //   interpolation for the time it now represents.
        var firstEntry = mTimedValues.First();
        var oldBeginTime = firstEntry.mEndTime;
        if (oldBeginTime < beginTime)
        {
          // Get data from second entry
          var secondEntry = mTimedValues.ElementAt(1);
          var oldEndTime = secondEntry.mEndTime;
          var endValue = secondEntry.mEndValue;
          var oldInterValue = secondEntry.mInterValue;

          // Calculate new values
          var oldBeginValue = 2 * oldInterValue - endValue;
          var timeFactor = (double)(beginTime - oldBeginTime) /
                            (double)(oldEndTime - oldBeginTime);
          var newBeginValue = oldBeginValue + timeFactor * (endValue - oldBeginValue);
          var newInterValue = (newBeginValue + endValue) / 2;

          // Modify the first two list entries with those new values
          mTimedValues.RemoveFirst();
          mTimedValues.RemoveFirst();
          secondEntry.mInterValue = newInterValue;
          mTimedValues.AddFirst(secondEntry);
          numberOfTimedValues--;
          if (beginTime < secondEntry.mEndTime)
          { // Re-add the first entry only if it has data for a non-zero length
            // time interval before the second
            firstEntry.mEndTime = beginTime;
            firstEntry.mEndValue = newBeginValue;
            mTimedValues.AddFirst(firstEntry);
            numberOfTimedValues++;
          }
        }
      }
      // Remove entries from the beginning of the list if we have too many, but
      // replace them with one averaged entry that covers their time
      if (numberOfTimedValues > mMaxEntries.Value)
      {
        // Split off excessive entries from the beginning
        var splitIndex = numberOfTimedValues - (mMaxEntries.Value - 2);
        LinkedList<TimedValue> tempValues = splitBefore(splitIndex, mTimedValues);
        // Calculate the average of those split-off entries
        var avg = getAvg(tempValues);
        // Use endTime and endValue from last split entry, but new average
        var newValue1 = tempValues.Last();
        newValue1.mInterValue = avg;
        mTimedValues.AddFirst(newValue1);
        // Save first split entry unchanged because
        // - endTime is begin time for average calculation,
        // - endValue is used for change calculation
        var newValue2 = tempValues.First();
        mTimedValues.AddFirst(newValue2);
      }
      return returnTime;
    }

    LinkedList<TimedValue> splitBefore(int splitIndex,
                    LinkedList<TimedValue> remainingValues)
    {
      LinkedList<TimedValue> splitValues = new LinkedList<TimedValue>();
      for ( int i = 0; i < splitIndex; i++ )
      {
        TimedValue moveValue = remainingValues.First();
        remainingValues.RemoveFirst();
        splitValues.AddLast(moveValue);
      }
      return splitValues;
    }

    /// <summary>
    /// Append a new entry to the end of the list, if it is a valid number that differs
    /// sufficiently from the previous entry's end value.
    /// </summary>
    /// <param name="endTime">The end time of the considered timespan.</param>
    /// <param name="endValue">The value received with timestamp endTime.</param>
    private void appendIfValid(long endTime, double endValue)
    {
      bool valueIsValid = !Double.IsNaN(endValue);
      if (valueIsValid)
      {
        // Average the current and the previous value, for use in the new list entry.
        // This means that when we later calculate the time-weighted average of the
        // list entries, based on such pre-averaged values, we will effectively have
        // linearly interpolated between received values.
        var previousValue = (mTimedValues.Count > 0) ? mTimedValues.Last().mEndValue : endValue;
        if (mTimedValues.Count > 1)
        {
          var beforeValue = mTimedValues.ElementAt(mTimedValues.Count - 2).mEndValue;
          if ( (Math.Abs(endValue - beforeValue) < (mInputResolution - EPSILON)) &&
               (Math.Abs(endValue - previousValue) < (mInputResolution - EPSILON)) )
          {
            mTimedValues.RemoveLast(); // close enough to the new one
            previousValue = mTimedValues.Last().mEndValue;
          }
        }
        if (valueIsValid)
        {
          var interValue = (endValue + previousValue) / 2;
          mTimedValues.AddLast(new TimedValue(endTime, interValue, endValue));
        }
      }
    }

    /// <summary>
    /// Set the output(s) according to the last list entry (if any)
    /// </summary>
    private void setOutputsFromLastValue()
    {
      var numTimedValues = mTimedValues.Count();

      if (numTimedValues > 0)
      {
        mOutputAvg.Value = mTimedValues.Last().mEndValue;
        mOutputMin.Value = mTimedValues.Last().mEndValue;
        mOutputMax.Value = mTimedValues.Last().mEndValue;
      }
      // else these outputs remain unchanged

      // Flatten trend to 0, if one was set before
      if (mOutputTrend.HasValue)
      {
        mOutputTrend.Value = 0;
      }

      // Flatten change to 0, if one was set before
      if (mOutputChange.HasValue)
      {
        mOutputChange.Value = 0.0;
      }
    }

    /// <summary>
    /// Set the output(s) to the selected function(s) of the list entries (if any
    /// exist), with constant extrapolation from the last entry if necessary.
    /// </summary>
    /// <param name="endTime">The end time of the considered timespan.</param>
    /// <param name="value">The end value to use for direct min/max update.</param>
    private void updateOutputs(long endTime, double value = Double.NaN)
    {
      updateAvgMinMax(endTime, value);  // looks at all entries
      updateTrend();                    // looks at last few entries 
      updateChange();                   // looks at first and last entry 
    }

    /// <summary>
    /// Set the Average, Minimum, and Maximum output(s) based on the given value
    /// and all list entries (if any exist). If no entry for the given end time
    /// exists, uses constant extrapolation from the last entry.
    /// </summary>
    /// <param name="endTime">The end time of the considered timespan.</param>
    /// <param name="value">The end value to use for direct min/max update.</param>
    private void updateAvgMinMax(long endTime, double value = Double.NaN)
    {
      var isMinMaxDirectUpdate = isUnlimitedTimeConsidered();
      if (isMinMaxDirectUpdate && !Double.IsNaN(value))
      {
        mOutputMin.Value = mOutputMin.HasValue ? Math.Min(mOutputMin.Value, value)
                                             : value;
        mOutputMax.Value = mOutputMax.HasValue ? Math.Max(mOutputMax.Value, value)
                                             : value;
      }
      // else min and max will be updated along the way while calculating average
      updateAvgFromTimedValues(!isMinMaxDirectUpdate, endTime);
    }

    /// <summary>
    /// Update the average output, and optionally the Minimum and Maximum outputs,
    /// from all list entries.
    /// <param name="doUpdateMinMax">True if min/max outputs must be updated.</param>
    /// <param name="endTime">The end time of the considered timespan.</param>
    private void updateAvgFromTimedValues(bool doUpdateMinMax, long endTime)
    {
      double avg = getAvg(mTimedValues, doUpdateMinMax, endTime);
      if ( !Double.IsNaN(avg) )
      {
        mOutputAvg.Value = avg;
      }
    }

    /// <summary>
    /// Update the average output, and optionally the Minimum and Maximum outputs,
    /// from all list entries.
    /// <param name="doUpdateMinMax">True if min/max outputs must be updated.</param>
    /// <param name="endTime">The end time of the considered timespan.</param>
    private double getAvg(LinkedList<TimedValue> localValues,
                                            bool doUpdateMinMax = false,
                                            long endTime = 0)
    {
      double avg = Double.NaN;
      var numberOfValues = localValues.Count();
      if (numberOfValues > 1)
      {
        if (0 == endTime)
        {
          endTime = localValues.Last().mEndTime;
        }
        var firstEntry = localValues.First();
        var beginTime = firstEntry.mEndTime;
        var totalDuration = endTime - beginTime;

        var sum = 0.0;
        var min = firstEntry.mEndValue;
        var max = min;

        foreach (TimedValue timedValue in localValues)
        {
          // Integrate current entry
          var duration = timedValue.mEndTime - beginTime;
          sum += timedValue.mInterValue * duration;

          // Calculate minimum and maximum
          min = Math.Min(min, timedValue.mEndValue);
          max = Math.Max(max, timedValue.mEndValue);

          // Prepare for integrating next entry
          beginTime = timedValue.mEndTime;
        }
        // If we have not enough data at the end, assume constant value after last entry
        if (beginTime < endTime)
        {
          var lastValue = localValues.Last().mEndValue;
          sum += lastValue * (endTime - beginTime);
        }
        avg = sum / totalDuration;
        if (doUpdateMinMax)
        {
          mOutputMin.Value = min;
          mOutputMax.Value = max;
        }
      }
      else if (numberOfValues > 0)
      {
        var val = localValues.First();
        avg = val.mInterValue;
        if (doUpdateMinMax)
        {
          mOutputMin.Value = val.mEndValue;
          mOutputMax.Value = val.mEndValue;
        }
      }
      // else all values remain unchanged
      return avg;
    }

    /// <summary>
    /// Look at the last few entries and the average to determine a trend
    /// (falling => -1; constant or unstable => 0; rising => 1)
    /// the scheduler service.
    /// </summary>
    private void updateTrend() 
    {
      var minNumberOfDifferencesToUse = 2;
      var maxNumberOfDifferencesToUse = 5;
      var numberOfTimedValues = mTimedValues.Count();
      if (numberOfTimedValues > minNumberOfDifferencesToUse)
      {
        int trend = 0;
        double accumulatedDifference = 0.0;
        var numberOfDifferencesToUse =
          Math.Min(maxNumberOfDifferencesToUse, numberOfTimedValues-1);
        var minIndex = numberOfTimedValues - numberOfDifferencesToUse - 1;
        var maxIndex = minIndex + numberOfDifferencesToUse;
        for (var i = minIndex; i < maxIndex; i++)
        {
          var recentDifference = mTimedValues.ElementAt(i + 1).mEndValue -
                                 mTimedValues.ElementAt(i).mEndValue;
          if (Math.Abs(recentDifference) > (2*mInputResolution - EPSILON))
          {
            trend += Math.Sign(recentDifference);
          }
          else
          {
            accumulatedDifference += recentDifference;
          }
          if (Math.Abs(accumulatedDifference) > (3 * mInputResolution - EPSILON))
          {
            trend += Math.Sign(recentDifference);
            accumulatedDifference = 0.0;
          }
        }
        mOutputTrend.Value = Math.Sign(trend);
      }
    }

    /// <summary>
    /// Update the change value.
    /// </summary>
    private void updateChange()
    {
      var numberOfTimedValues = mTimedValues.Count();
      if (numberOfTimedValues > 1)
      {
        // Use interpolated value from first entry because
        mOutputChange.Value = mTimedValues.Last().mEndValue - mTimedValues.First().mEndValue;
      }
      else if (mOutputChange.HasValue)
      {
        mOutputChange.Value = 0.0;
      }
    }

    /// <summary>
    /// If the output should be updated automatically, add the next trigger time to
    /// the scheduler service.
    /// </summary>
    private void reschedule()
    {
      if ( (mTimedValues.Count > 0) && (mUpdateToken == null) &&
           (mUpdateTime.HasValue) && (mUpdateTime.Value.Ticks > 0) )
      {
        mUpdateToken = mSchedulerService.InvokeIn(mUpdateTime.Value, Trigger);
      }
    }

    /// <summary>
    /// Unschedule the next trigger execution, if scheduled.
    /// </summary>
    private void unschedule()
    {
      if (mUpdateToken != null)
      {
        mSchedulerService.Remove(mUpdateToken);
        mUpdateToken = null;
      }
    }
  }
}
