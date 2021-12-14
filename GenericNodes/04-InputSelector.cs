using System.Collections.Generic;

using LogicModule.Nodes.Helpers;
using LogicModule.ObjectModel;
using LogicModule.ObjectModel.TypeSystem;

namespace Recomedia_de.Logic.Generic
{
  /// <summary>
  /// Out of many inputs, pass the selected to the output.
  /// </summary>
  /// <remarks>
  /// Unlike the version Gira provides with their standard input selector,
  /// this version, allows to, when the input selection changes, re-send the
  /// previous value from the newly selected input.
  /// </remarks>
  public class InputSelector : LocalizablePrefixLogicNodeBase
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
    public InputSelector(INodeContext context)
      : base(context, INPUT_PREFIX)
    {
      context.ThrowIfNull("context");
      mTypeService = context.GetService<ITypeService>();

      // Initialize the input count, allowing range 2..50.
      mInputCount = mTypeService.CreateInt(PortTypes.Integer, "InputCount", 2);
      mInputCount.MinValue = 2;
      mInputCount.MaxValue = 50;

      // Initialize inputs using a helper function that grows/shrinks the list of inputs
      // whenever the input count is changed
      mInputs = new List<AnyValueObject>();
      ListHelpers.ConnectListToCounter(mInputs, mInputCount,
          mTypeService.GetValueObjectCreator(PortTypes.Any, INPUT_PREFIX),
          updateOutputValues);

      // Initialize the input for the index of the input to select.
      mSelectIndexInput = mTypeService.CreateInt(PortTypes.Integer, "InputSelectIndex");
      mSelectIndexInput.MinValue = -1;
      mSelectIndexInput.MaxValue = mInputCount.MaxValue;
      mSelectIndexInput.ValueSet += updateOutputValues;

      // Initialize the selector for "send-on-select".
      mSelectAction = mTypeService.CreateEnum("ESelectAction2",
          "SelectAction2", mSelectActionValues, "ResendCurrent");

      // Initialize the output
      mOutput = mTypeService.CreateAny(PortTypes.Any, "Output");
    }

    /// <summary>
    /// A list of input values. 
    /// </summary>
    [Input(DisplayOrder = 1, InitOrder = 2, IsDefaultShown = true)]
    public IList<AnyValueObject> mInputs { get; private set; }

    /// <summary>
    /// How many inputs shall be used.
    /// </summary>
    [Parameter(DisplayOrder = 2, InitOrder = 1, IsDefaultShown = false)]
    public IntValueObject mInputCount { get; private set; }

    /// <summary>
    /// The selection index input. 
    /// </summary>
    [Input(DisplayOrder = 3, InitOrder = 3, IsDefaultShown = true)]
    public IntValueObject mSelectIndexInput { get; private set; }

    /// <summary>
    /// What to do when the selected input changes.
    /// </summary>
    [Parameter(DisplayOrder = 4, InitOrder = 4, IsDefaultShown = false)]
    public EnumValueObject mSelectAction { get; private set; }
    private readonly string[] mSelectActionValues =
        { "ResendCurrent", "ResendNothing" };

    /// <summary>
    /// The output for the selected input value.
    /// </summary>
    [Output(DisplayOrder = 5, IsRequired = true)]
    public AnyValueObject mOutput { get; private set; }

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
      // We are only interested in changes of the input selection, and the
      // selected input's value (not all the other input values)
      int selInpIdx = getSelectedInputIndex();
      if (selInpIdx >= 0)
      {
        // Update the output
        bool doResendUponSelect = getResendUponSelect();
        var selInp = mInputs[selInpIdx];
        if ( (selInp.WasSet) ||
             (mSelectIndexInput.WasSet && doResendUponSelect) )
        {
          if (selInp.HasValue)
          {
            mOutput.Value = selInp.Value;
          }
        }
        // Reset the inputs
        mSelectIndexInput.WasSet = false;
        foreach (var input in mInputs)
        {
          input.WasSet = false;
        }
      }
    }

    private int getSelectedInputIndex()
    {
      if ( (mSelectIndexInput.HasValue) &&
           (mSelectIndexInput.Value >= 0) &&
           (mSelectIndexInput.Value < mInputs.Count) )
      {
        return mSelectIndexInput.Value;
      }
      return -1;
    }

    private bool getResendUponSelect()
    {
      if (mSelectIndexInput.WasSet && mSelectAction.HasValue)
      {
        return ("ResendCurrent" == mSelectAction.Value);
      }
      return false;
    }
  }
}
