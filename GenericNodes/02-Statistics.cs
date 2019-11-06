using System;
using System.Collections.Generic;
using System.Linq;
using LogicModule.Nodes.Helpers;
using LogicModule.ObjectModel;
using LogicModule.ObjectModel.TypeSystem;

namespace Recomedia_de.Logic.Generic
{
  /// <summary>
  /// Calculate statistical functions of multiple input values.
  /// </summary>
  /// <remarks>
  /// Unlike the version Gira provides with their Logic Node SDK "Aggregation" example,
  /// this version
  /// - allows to calculate indices of minimum and maximum inputs, and standard
  ///   deviation,
  /// - does not throw exceptions when one or more of the inputs have no values
  ///   yet.
  /// To keep the required processing time low, only user-selected values (up to two)
  /// are calculated (not like the example does, all of them, even for unused/disabled
  /// outputs).
  /// </remarks>
  public class Statistics : LocalizablePrefixLogicNodeBase
  {
    /// <summary>
    /// The prefix to use for the names of the inputs. An index is added afterwards to
    /// give a unique name.
    /// </summary>
    private const string INPUT_PREFIX = "Input";

    /// <summary>
    /// The TypeService is saved to allow updating after the constructor is finished.
    /// </summary>
    private readonly ITypeService mTypeService;

    /// <summary>
    /// Initializes a new instance of the <see cref="BasicNode"/> class.
    /// </summary>
    /// <param name="context">The node context.</param>
    public Statistics(INodeContext context)
      : base(context, INPUT_PREFIX)
    {
      context.ThrowIfNull("context");
      mTypeService = context.GetService<ITypeService>();

      // Initialize the input count.
      mInputCount = mTypeService.CreateInt(PortTypes.Integer, "InputCount", 2);
      mInputCount.MinValue = 2;
      mInputCount.MaxValue = 50;

      // Initialize inputs using a helper function that grows/shrinks the list of inputs
      // whenever the input count is changed
      mInputs = new List<DoubleValueObject>();
      ListHelpers.ConnectListToCounter(mInputs, mInputCount,
          mTypeService.GetValueObjectCreator(PortTypes.Number, INPUT_PREFIX),
          updateOutputValues);

      // Initialize the selector for the aggregate function that shall be calculated.
      mSelectedFunction = mTypeService.CreateEnum("ESelectedFunction",
          "SelectedFunction", mPriAllowedValues, /* defaultValue = */ "Avg");

      // Initialize the primary output
      mOutput1 = mTypeService.CreateDouble(PortTypes.Number, "Output1");

      // Initialize the selector for the secondary function (none by default) that shall
      // be calculated.
      string[] secAllowedValues = mAddAllowedValues.Concat(mPriAllowedValues).ToArray();
      mSecondaryFunction = mTypeService.CreateEnum("ESecondaryFunction",
          "SecondaryFunction", secAllowedValues, /* defaultValue = */ "none");

      // Update output properties now, and whenever the function selections change
      updatePrimaryOutputProperties();
      updateSecondaryOutputProperties();
      mSelectedFunction.ValueSet  += updatePrimaryOutputProperties;
      mSecondaryFunction.ValueSet += updateSecondaryOutputProperties;
    }

    /// <summary>
    /// A list of double value objects as inputs. 
    /// </summary>
    [Input(DisplayOrder = 1, InitOrder = 2, IsDefaultShown = true)]
    public IList<DoubleValueObject> mInputs { get; private set; }

    /// <summary>
    /// How many inputs shall be used.
    /// </summary>
    [Parameter(DisplayOrder = 2, InitOrder = 1, IsDefaultShown = false)]
    public IntValueObject mInputCount { get; private set; }

    /// <summary>
    /// Which primary aggregate function shall be calculated.
    /// </summary>
    [Parameter(DisplayOrder = 4, InitOrder = 4, IsDefaultShown = false)]
    public EnumValueObject mSelectedFunction { get; private set; }
    private readonly string[] mPriAllowedValues = { "Min", "Max", "Sum", "Avg", "StdDev" };

    /// <summary>
    /// The calculated primary function value of all input values.
    /// </summary>
    [Output(DisplayOrder = 5, IsRequired = true)]
    public DoubleValueObject mOutput1 { get; private set; }

    /// <summary>
    /// Which secondary function (if any) shall be calculated.
    /// </summary>
    [Parameter(DisplayOrder = 7, InitOrder = 7, IsDefaultShown = false)]
    public EnumValueObject mSecondaryFunction { get; private set; }
    private readonly string[] mAddAllowedValues = { "none", "MinIndex", "MaxIndex" };

    /// <summary>
    /// The calculated secondary function value (if any) of all input values.
    /// </summary>
    [Output(DisplayOrder = 8, IsRequired = true)]
    public DoubleValueObject mOutput2 { get; private set; }

    /// <summary>
    /// The calculated secondary index (if any) of the relevant input value.
    /// </summary>
    [Output(DisplayOrder = 10, IsRequired = true)]
    public IntValueObject mOutputIndex { get; private set; }

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
      if (mSecondaryFunction.Value == mSelectedFunction.Value)
      {
        return new ValidationResult
        {
          HasError = true,
          Message = Localize(language, "SameFunction")
        };
      }
      return base.Validate(language);
    }

    /// <summary>
    /// Called when the PrimaryFunction receives a new value, in order to update which
    /// of the outputs exist, and which labels they have.
    /// </summary>
    private void updatePrimaryOutputProperties(object sender = null,
                                ValueChangedEventArgs evArgs = null)
    {
      mOutput1.Name = mSelectedFunction.Value;
    }

    /// <summary>
    /// Called when the SecondaryFunction receives a new value, in order to update which
    /// of the outputs exist, and which labels they have.
    /// </summary>
    private void updateSecondaryOutputProperties(object sender = null,
                                  ValueChangedEventArgs evArgs = null)
    {
      if ( new List<string> { "Min", "Max", "Sum", "Avg", "StdDev" }.
           Contains(mSecondaryFunction.Value) )
      {
        // Add secondary value output; remove secondary index output
        mOutput2 = mTypeService.CreateDouble(PortTypes.Number,
                                     mSecondaryFunction.Value);
        mOutputIndex = null;
      }
      else if ( new List<string>{"MinIndex", "MaxIndex"}.
                Contains(mSecondaryFunction.Value) )
      {
        // Remove secondary value output; add secondary index output
        mOutput2 = null;
        mOutputIndex = mTypeService.CreateInt(PortTypes.Integer,
                                       mSecondaryFunction.Value);
      }
      else
      {
        // Remove both secondary outputs
        mOutput2 = null;
        mOutputIndex = null;
      }
    }

    /// <summary>
    /// This method has been added as a ValueSet handler to each input. It will
    /// therefore be called when ANY of the inputs receives a value, in order
    /// to update the output. Usually this would be done by overriding the
    /// Execute method, but unfortunately Execute is not called before ALL
    /// inputs have values. 
    /// </summary>
    /// <remarks>
    /// This method is public only for testability reasons
    /// </remarks>
    public void updateOutputValues(object sender = null,
                     ValueChangedEventArgs evArgs = null)
    {
      if (mOutput1 != null)
      {
        double val = calculateValue(mSelectedFunction.Value);
        if ( !Double.IsNaN(val) )
        {
          mOutput1.Value = val;
        }
      }
      if (mOutput2 != null)
      {
        double val = calculateValue(mSecondaryFunction.Value);
        if (!Double.IsNaN(val))
        {
          mOutput2.Value = val;
        }
      }
      if (mOutputIndex != null)
      {
        int val = calculateIndex(mSecondaryFunction.Value);
        if ( val >= 0 )
        {
          mOutputIndex.Value = val;
        }
      }
    }

    /// <summary>
    /// Calculate the value for the given function
    /// </summary>
    private double calculateValue(string what)
    {
      if ( "Min" == what )
      {
        return getMin();
      }
      else if ( "Max" == what )
      {
        return getMax();
      }
      else if ( "Sum" == what )
      {
        return getSum();
      }
      else if ( "Avg" == what )
      {
        return getAverage();
      }
      else if ( "StdDev" == what )
      {
        return getStandardDeviation();
      }
      return 0.0;
    }

    /// <summary>
    /// Calculate the given index function
    /// </summary>
    private int calculateIndex(string what)
    {
      if ( "MinIndex" == what )
      {
        // Calculate the index of the input that has the smallest value
        return getMinIndex();
      }
      else if ( "MaxIndex" == what )
      {
        // Calculate the index of the input that has the largest value
        return getMaxIndex();
      }
      return -1;
    }

    /// <summary>
    /// Calculate the index of the input that has the smallest value
    /// </summary>
    private int getMinIndex()
    {
      int returnIndex = -1;
      double indexValue = 0;
      foreach ( var item in mInputs.Select((input, index) => new { input, index }) )
      {
        if ( item.input.HasValue &&
             ( (item.input.Value < indexValue) || (returnIndex < 0) ) )
        {
          returnIndex = item.index;
          indexValue = item.input.Value;
        }
      }
      return returnIndex;
    }

    /// <summary>
    /// Calculate the index of the input that has the largest value
    /// </summary>
    private int getMaxIndex()
    {
      int returnIndex = -1;
      double indexValue = 0;
      foreach ( var item in mInputs.Select((input, index) => new { input, index }) )
      {
        if ( item.input.HasValue &&
             ( (item.input.Value > indexValue) || (returnIndex < 0) ) )
        {
          returnIndex = item.index;
          indexValue = item.input.Value;
        }
      }
      return returnIndex;
    }

    /// <summary>
    /// Select the value of the input that has the smallest value
    /// </summary>
    private double getMin()
    {
      bool minValueValid = false;
      double minValue = Double.NaN;
      foreach ( var input in mInputs )
      {
        if ( input.HasValue &&
             ( (input.Value < minValue) || !minValueValid ) )
        {
          minValueValid = true;
          minValue = input.Value;
        }
      }
      return minValue;
    }

    /// <summary>
    /// Select the value of the input that has the largest value
    /// </summary>
    private double getMax()
    {
      bool maxValueValid = false;
      double maxValue = Double.NaN;
      foreach ( var input in mInputs )
      {
        if ( input.HasValue &&
             ( (input.Value > maxValue) || !maxValueValid ) )
        {
          maxValueValid = true;
          maxValue = input.Value;
        }
      }
      return maxValue;
    }

    /// <summary>
    /// Calculate the sum of all inputs
    /// </summary>
    private double getSum()
    {
      int numberOfValues = 0;
      double sum = getSumAndNumberOfValues(ref numberOfValues);
      if ( numberOfValues <= 0 )
      {
        sum = Double.NaN;
      }
      return sum;
    }

    /// <summary>
    /// Calculate the average of all inputs
    /// </summary>
    private double getAverage()
    {
      int numberOfValuesDummy = 0;
      double average = getAverageAndNumberOfValues(ref numberOfValuesDummy);
      return average;
    }

    /// <summary>
    /// Calculate the standard deviation of all inputs, assuming the
    /// inputs are the whole population (as opposed to a sample)
    /// </summary>
    private double getStandardDeviation()
    {
      int numberOfValues = 0;
      double stdDev = Double.NaN;
      double average = getAverageAndNumberOfValues(ref numberOfValues);
      if (numberOfValues > 0)
      {
        double sumOfSquaresOfDifferences = getSumOfSquaresOfDifferences(average);
        stdDev = System.Math.Sqrt(sumOfSquaresOfDifferences / numberOfValues);
      }
      return stdDev;
    }

    /// <summary>
    /// Calculate the average of all inputs
    /// </summary>
    private double getAverageAndNumberOfValues(ref int numberOfValues)
    {
      numberOfValues = 0;
      double average = Double.NaN;
      double sum = getSumAndNumberOfValues(ref numberOfValues);
      if ( numberOfValues > 0 )
      {
        average = sum / numberOfValues;
      }
      return average;
    }

    /// <summary>
    /// Calculate the sum of all inputs, and the number of values along the way.
    /// (Some inputs could have no values yet.)
    /// </summary>
    private double getSumAndNumberOfValues(ref int numberOfValues)
    {
      double sum = 0;
      numberOfValues = 0;
      foreach ( var input in mInputs )
      {
        if ( input.HasValue )
        {
          sum += input.Value;
          numberOfValues++;
        }
      }
      return sum;
    }

    /// <summary>
    /// Used in standard deviation calculation.
    /// </summary>
    private double getSumOfSquaresOfDifferences(double average)
    {
      double sum = 0;
      foreach ( var input in mInputs )
      {
        if ( input.HasValue )
        {
          sum += (input.Value - average) * (input.Value - average);
        }
      }
      return sum;
    }
  }
}
