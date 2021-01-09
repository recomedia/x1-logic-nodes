using System.Collections.Generic;
using System.Linq;

using LogicModule.Nodes.Helpers;
using LogicModule.ObjectModel;
using LogicModule.ObjectModel.TypeSystem;

namespace Recomedia_de.Logic.Generic
{
  /// <summary>
  /// Pass the input to one out of many outputs.
  /// </summary>
  /// <remarks>
  /// When the input selection changes, an "idle" value can be sent to the de-
  /// selected output, and/or the last value sent before de-selection can be
  /// sent to the selected output.
  /// </remarks>
  public class OutputSelector : LocalizablePrefixLogicNodeBase
  {
    /// <summary>
    /// The prefix to use for the names of the outputs. An index is added to
    /// give a unique name.
    /// </summary>
    private const string OUTPUT_PREFIX = "Output";

    /// <summary>
    /// The TypeService is saved to allow updating after the constructor is finished.
    /// </summary>
    private readonly ITypeService mTypeService;

    /// <summary>
    /// Initializes a new instance of the <see cref="BasicNode"/> class.
    /// </summary>
    /// <param name="context">The node context.</param>
    public OutputSelector(INodeContext context)
      : base(context, OUTPUT_PREFIX)
    {
      context.ThrowIfNull("context");
      mTypeService = context.GetService<ITypeService>();

      // Initialize the input.
      mInput = mTypeService.CreateAny(PortTypes.Any, "Input");
      mInput.ValueSet += updateOutputValues;

      // Initialize the output count.
      mOutputCount = mTypeService.CreateInt(PortTypes.Integer, "OutputCount", 2);
      mOutputCount.MinValue = 1;  // for use as Lock-out+ (Sperre+)
      mOutputCount.MaxValue = 50;

      // Initialize the input for the index of the output to select.
      mSelectIndexInput = mTypeService.CreateInt(PortTypes.Integer, "OutputSelectIndex");
      mSelectIndexInput.MinValue = -1;                    //  -1 selects no output
      mSelectIndexInput.MaxValue = mOutputCount.MaxValue; // max selects no output
      mSelectIndexInput.ValueSet += updateOutputValues;

      // Initialize the selector for "send-on-select".
      mSelectAction = mTypeService.CreateEnum("ESelectAction3",
          "SelectAction3", mSelectActionValues, "ResendCurrent");

      // Initialize outputs using a helper function that grows/shrinks the list of outputs
      // whenever the output count is changed
      mOutputs = new List<AnyValueObject>();
      ListHelpers.ConnectListToCounter(mOutputs, mOutputCount,
          mTypeService.GetValueObjectCreator(PortTypes.Any, OUTPUT_PREFIX), null);

      // Initialize outputs using a helper function that grows/shrinks the
      // list of outputs whenever the output count is changed
      mPrevOutputs = new List<AnyValueObject>();
      ListHelpers.ConnectListToCounter(mPrevOutputs, mOutputCount,
          mTypeService.GetValueObjectCreator(PortTypes.Any, ""), null, null);

      // Initialize the value to send upon "de-select".
      mIdleValueType = mTypeService.CreateEnum("EValueType",
          "IdleValueType", mValueTypes, "none");
      updateIdleValueType();
      mIdleValueType.ValueSet += updateIdleValueType;
    }

    /// <summary>
    /// The input to pass to the selected output.
    /// </summary>
    [Input(DisplayOrder = 1)]
    public AnyValueObject mInput { get; private set; }

    /// <summary>
    /// A list of output values. 
    /// </summary>
    [Output(DisplayOrder = 1, IsDefaultShown = true)]
    public IList<AnyValueObject> mOutputs { get; private set; }

    /// <summary>
    /// How many outputs shall be used.
    /// </summary>
    [Parameter(DisplayOrder = 3, InitOrder = 1, IsDefaultShown = false)]
    public IntValueObject mOutputCount { get; private set; }

    /// <summary>
    /// The selection index input. 
    /// </summary>
    [Input(DisplayOrder = 2, InitOrder = 3, IsRequired = true)]
    public IntValueObject mSelectIndexInput { get; private set; }

    /// <summary>
    /// What to send to an output that changes from deselected to selected.
    /// </summary>
    [Parameter(DisplayOrder = 4, InitOrder = 4, IsDefaultShown = false)]
    public EnumValueObject mSelectAction { get; private set; }
    private readonly string[] mSelectActionValues =
        { "ResendCurrent", "ResendPrevious", "ResendNothing" };

    /// <summary>
    /// The value (if any) to send to an output that changes from selected to deselected.
    /// </summary>
    [Parameter(DisplayOrder = 6, InitOrder = 6, IsDefaultShown = false)]
    public EnumValueObject mIdleValueType { get; private set; }
    private readonly string[] mValueTypes = { "none",
                                              PortTypes.Bool, PortTypes.Integer,
                                              PortTypes.Number, PortTypes.String,
                                              PortTypes.TimeSpan, PortTypes.Time,
                                              PortTypes.Date, PortTypes.DateTime };

    /// <summary>
    /// The value (if any) to send to an output that changes from selected to deselected.
    /// </summary>
    [Parameter(DisplayOrder = 7, InitOrder = 7, IsDefaultShown = false)]
    public IValueObject mIdleValue { get; private set; }

    /// <summary>
    /// The index of the previously selected output.
    /// </summary>
    private int mPrevSelOutpIdx = -1;

    /// <summary>
    /// The values of outputs upon last deselection.
    /// </summary>
    private IList<AnyValueObject> mPrevOutputs;

    /// <summary>
    /// This method provides the hook to implement startup logic. It is called after
    /// the event processing has started. We use it to forward the idle value to all
    /// outputs at the beginning.
    /// </summary>
    public override void Startup()
    {
      // If given, send "idle" value to all outputs
      if ( (null != mIdleValue) && mIdleValue.HasValue )
      {
        foreach ( var output in mOutputs )
        {
          output.Value = mIdleValue.Value;
        }
      }
      // If initially set, use input value for all "re-send" values
      if ( mInput.HasValue )
      {
        foreach ( var lastInput in mPrevOutputs )
        {
          lastInput.Value = mInput.Value;
        }
      }
    }

    /// <summary>
    /// Called when the SecondaryFunction receives a new value, in order to update which
    /// of the outputs exist, and which labels they have.
    /// </summary>
    private void updateIdleValueType(object sender = null,
                      ValueChangedEventArgs evArgs = null)
    {
      System.Diagnostics.Trace.Assert(mIdleValueType.HasValue);

      if (PortTypes.Bool == mIdleValueType.Value)
      {
        mIdleValue = mTypeService.CreateBool(PortTypes.Bool, "IdleValue");
      }
      else if (PortTypes.Integer == mIdleValueType.Value)
      {
        mIdleValue = mTypeService.CreateInt(PortTypes.Integer, "IdleValue");
      }
      else if (PortTypes.Number == mIdleValueType.Value)
      {
        mIdleValue = mTypeService.CreateDouble(PortTypes.Number, "IdleValue");
      }
      else if (PortTypes.TimeSpan == mIdleValueType.Value)
      {
        mIdleValue = mTypeService.CreateTimeSpan(PortTypes.TimeSpan, "IdleValue");
      }
      else if (PortTypes.Time == mIdleValueType.Value)
      {
        mIdleValue = mTypeService.CreateTimeSpan(PortTypes.Time, "IdleValue");
      }
      else if (PortTypes.Date == mIdleValueType.Value)
      {
        mIdleValue = mTypeService.CreateDateTime(PortTypes.Date, "IdleValue");
      }
      else if (PortTypes.DateTime == mIdleValueType.Value)
      {
        mIdleValue = mTypeService.CreateDateTime(PortTypes.DateTime, "IdleValue");
      }
      else if (PortTypes.String == mIdleValueType.Value)
      {
        mIdleValue = mTypeService.CreateString(PortTypes.String, "IdleValue");
      }
      else
      {
        mIdleValue = null;
      }
    }

    /// <summary>
    /// This method has been added as a ValueSet handler to each input. It will
    /// therefore be called when ANY of the inputs receives a value, in order
    /// to update the output. Usually this would be done by overriding the
    /// Execute method, but unfortunately Execute is not called before ALL
    /// inputs have values. 
    /// </summary>
    private void updateOutputValues(object sender = null,
                     ValueChangedEventArgs evArgs = null)
    {
      string resendUponSelect = getResendUponSelect();
      int selOutpIdx = getSelectedOutputIndex();

      if ( (mPrevSelOutpIdx != selOutpIdx) && (mPrevSelOutpIdx >= 0) )
      {
        // The selected output has indeed been changed (as opposed to just set to
        // the same value, or initially set). We therefore deal with de-selection
        // of the previously selected output.
        if ( mOutputs[mPrevSelOutpIdx].HasValue &&
             ("ResendPrevious" == resendUponSelect) )
        {
          // If needed and possible, save current value of output being deselected,
          // for later re-send
          mPrevOutputs[mPrevSelOutpIdx].Value = mOutputs[mPrevSelOutpIdx].Value;
        }
        if ( (null != mIdleValue) && mIdleValue.HasValue )
        {
          // If given, send "idle" value to the output being deselected
          mOutputs[mPrevSelOutpIdx].Value = mIdleValue.Value;
        }
      }

      if (selOutpIdx >= 0)
      {
        // We have a valid output index, so let's update this output as needed
        if ( mInput.WasSet && mInput.HasValue )
        {
          // Pass new input value to output
          mOutputs[selOutpIdx].Value = mInput.Value;
        }
        else if ( mSelectIndexInput.WasSet &&
                  (mPrevSelOutpIdx != selOutpIdx) )
        {
          // If requested, and if the input didn't receive a new value, and if this
          // output wasn't already selected, resend a value
          switch ( resendUponSelect )
          {
            case "ResendPrevious":
              var lastBeforeDeselect = mPrevOutputs[selOutpIdx];
              if (lastBeforeDeselect.HasValue)
              {
                mOutputs[selOutpIdx].Value = lastBeforeDeselect.Value;
              }
              break;
            case "ResendCurrent":
              if (mInput.HasValue)
              {
                mOutputs[selOutpIdx].Value = mInput.Value;
              }
              break;
          }
        }
      }
      // Memorize selected output index, to later check for changes, and update deselected
      // outputs
      mPrevSelOutpIdx = selOutpIdx;

      // Reset the inputs
      mSelectIndexInput.WasSet = false;
      mInput.WasSet = false;
    }

    private int getSelectedOutputIndex()
    {
      if ( (mSelectIndexInput.HasValue) &&
           (mSelectIndexInput.Value < mOutputs.Count) )
      {
        return mSelectIndexInput.Value;
      }
      return -1;
    }

    private string getResendUponSelect()
    {
      if (mSelectIndexInput.WasSet && mSelectAction.HasValue)
      {
        return mSelectAction.Value;
      }
      return "ResendNothing";
    }
  }
}
