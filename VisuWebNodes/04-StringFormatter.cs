using System;
using System.Collections.Generic;
using LogicModule.Nodes.Helpers;
using LogicModule.ObjectModel;
using LogicModule.ObjectModel.TypeSystem;

namespace Recomedia_de.Logic.VisuWeb
{
  /// <summary>
  /// Formats an output string based on a given template and an arbitrary
  /// number of value inputs. Inputs are created as needed based on
  /// placeholders in the template string.
  /// </summary>
  public class StringFormatter : PlaceholderNodeBase
  {
    protected override string TEMPLATE_PREFIX { get { return "Template"; } }

    /// <summary>
    /// Initializes a new instance of the <see cref="StringFormatter"/> class.
    /// </summary>
    /// <param name="context">The node context.</param>
    public StringFormatter(INodeContext context)
      : base(context)
    {
      // Initialize the group and decimal separator parameters.
      mCustomDecimalSeparator = mTypeService.CreateString(PortTypes.String, "SeparatorDecimal",
                                                                      /* defaultValue = */ ".");
      mCustomGroupSeparator = mTypeService.CreateString(PortTypes.String, "SeparatorGroup",
                                                                      /* defaultValue = */ "'");
      mCustomDecimalSeparator.ValueSet += updateTemplate;
      mCustomGroupSeparator.ValueSet += updateTemplate;

      // Initialize for default template count
      updateTemplateCount();
    }

    /// <summary>
    /// Parameters to customize the group and decimal separators.
    /// </summary>
    [Parameter(DisplayOrder = 40, InitOrder = 40, IsDefaultShown = false)]
    public StringValueObject mCustomDecimalSeparator { get; private set; }
    [Parameter(DisplayOrder = 41, InitOrder = 41, IsDefaultShown = false)]
    public StringValueObject mCustomGroupSeparator { get; private set; }

    protected override string getGroupSeparator()
    {
      return mCustomGroupSeparator;
    }

    protected override string getDecimalSeparator()
    {
      return mCustomDecimalSeparator;
    }

    protected override IValueObject createOutput(int i)
    {
      return mTypeService.CreateString(PortTypes.String, getOutputName(i));
    }

    protected override void updateOutputHelpers(int newCount, int oldCount)
    {
      // We have no other members that need to be updated along with outputs
    }

    protected override void updateTemplateHelpers()
    {
      // We have no other members that need to be updated along with templates
    }

    protected override ValidationResult validateTokens(string language,
                                                       string templateName,
                                          ref List<TokenBase> templateTokens)
    {
      // no additional validations; we fully support all token types
      return new ValidationResult { HasError = false };
    }

    /// <summary>
    /// This method has been added as a ValueSet handler to each input. It will
    /// therefore be called when ANY of the inputs receives a value, in order
    /// to update the output. Usually this would be done by overriding the
    /// Execute method, but unfortunately Execute is not called before ALL
    /// inputs have values, so we don't use it at all. 
    /// </summary>
    protected override void updateOutputValues(object sender = null,
                                ValueChangedEventArgs evArgs = null)
    {
      for (int i = 0; i < mTokensPerTemplate.Count; i++)
      {
        string outText = "";
        foreach (TokenBase token in mTokensPerTemplate[i])
        {
          outText += token.getText();
        }
        mOutputs[i].Value = outText;
      }
    }
  }
}
