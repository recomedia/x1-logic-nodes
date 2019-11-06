using LogicModule.Nodes.Helpers;
using LogicModule.ObjectModel;
using LogicModule.ObjectModel.TypeSystem;

namespace Recomedia_de.Logic.Generic
{
  /// <summary>
  /// Forwards the input to the output, if the new input value differs significantly
  /// (i. e. at least by the specified minimum difference) from the previous value
  /// </summary>
  /// <remarks>
  /// Unlike the Send-by-Change logic node Gira provides with their standard nodes,
  /// this version
  /// - allows to specify a minimum difference for sending the value, and
  /// - therefore works only with numbers, not with other types of input data.
  /// If a received input value is closer to the last sent output value than the
  /// "minimum difference" parameter, then it is not sent to the output.
  /// </remarks>
  public class SendByDifference : LogicNodeBase
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="SendByDifference"/> class.
    /// </summary>
    /// <param name="context">The node context.</param>
    public SendByDifference(INodeContext context)
      : base(context)
    {
      context.ThrowIfNull("context");
      var typeService = context.GetService<ITypeService>();

      // Initialize Input and Output with no defaults or restrictions
      mInput = typeService.CreateDouble(PortTypes.Number, "Input");
      mOutput = typeService.CreateDouble(PortTypes.Number, "Output");

      // Initialize MinimumDifference parameter with a default of 2.0,
      // and restrict to positive values
      mMinimumDifference = typeService.CreateDouble(PortTypes.Number,
                                           "MinimumDifference", 1.0);
      mMinimumDifference.MinValue = 0.0;
    }

    /// <summary>
    /// Input for values to filter.
    /// </summary>
    [Input(DisplayOrder = 1, IsRequired = true)] // Users cannot disable
    public DoubleValueObject mInput { get; private set; }

    /// <summary>
    /// Minimum difference to discard input values.
    /// </summary>
    [Parameter(DisplayOrder = 2, IsDefaultShown = true)]
    public DoubleValueObject mMinimumDifference { get; private set; }

    /// <summary>
    /// The filtered input value.
    /// </summary>
    [Output(DisplayOrder = 1, IsRequired = true)]
    public DoubleValueObject mOutput { get; private set; }

    /// <summary>
    /// Called when the input receives a new value or the reset input is set.
    /// </summary>
    public override void Execute()
    {
      if ( mInput.WasSet && mInput.HasValue )
      {
        if ( (!mOutput.HasValue) ||     // We have no output value at all yet, or ...
             (System.Math.Abs(mInput.Value - mOutput.Value) >= mMinimumDifference.Value) )
        {                               // ... the input value has changed significantly
          mOutput.Value = mInput.Value; // ==> we have to send the new value
        }
      }
    }
  }
}
