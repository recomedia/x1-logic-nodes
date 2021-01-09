using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Text.RegularExpressions;
using LogicModule.Nodes.Helpers;
using LogicModule.ObjectModel;
using LogicModule.ObjectModel.TypeSystem;

namespace Recomedia_de.Logic.VisuWeb
{
  /// <summary>
  /// Allows derived classes to create aritrary output values based
  /// on a given template and an arbitrary number of value inputs.
  /// Inputs are created as needed based on placeholders in the
  /// template string.
  /// </summary>
  public abstract class PlaceholderNodeBase : LogicNodeBase
  {
    /// <summary>
    /// The special characters and regular expression used to enclose/find
    /// variable fields (placeholders) in the template
    /// string.
    /// </summary>
    private static readonly char[] PLACEHOLDER_DELIMITERS = { '{', '}' };
    private static readonly Regex PLACEHOLDER_REGEX =
          new Regex(Char.ToString(PLACEHOLDER_DELIMITERS[0]) +
                    "[^" + Char.ToString(PLACEHOLDER_DELIMITERS[0]) +
                           Char.ToString(PLACEHOLDER_DELIMITERS[1]) + "]*" +
                    Char.ToString(PLACEHOLDER_DELIMITERS[1]));

    protected abstract string TEMPLATE_PREFIX { get; }
    private const string OUTPUT_PREFIX = "Output";
    private const string INPUT_BIN_PREFIX = "BinaryInput";
    private const string INPUT_INT_PREFIX = "IntegerInput";
    private const string INPUT_NUM_PREFIX = "NumberInput";
    private const string INPUT_STR_PREFIX = "StringInput";

    /// <summary>
    /// The allowable maximum number of inputs for each type.
    /// </summary>
    private const int MAX_INPUTS = 50;

    /// <summary>
    /// The group and decimal separator to use when formatting numbers as text can
    /// be customized by derived classes.
    /// </summary>
    private string mGroupSeparator = "'";
    protected virtual string getGroupSeparator()
    {
      return mGroupSeparator;
    }
    private string mDecimalSeparator = ".";
    protected virtual string getDecimalSeparator()
    {
      return mDecimalSeparator;
    }

    /// <summary>
    /// The TypeService is saved to allow updating after the constructor is finished.
    /// </summary>
    protected readonly ITypeService mTypeService;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaceholderNodeBase"/> class.
    /// </summary>
    /// <param name="context">The node context.</param>
    public PlaceholderNodeBase(INodeContext context)
      : base(context)
    {
      context.ThrowIfNull("context");
      mTypeService = context.GetService<ITypeService>();

      // Initialize one template parameter, but allow more of them
      mTemplateCount = mTypeService.CreateInt(PortTypes.Integer, TEMPLATE_PREFIX + "Count", 1);
      mTemplateCount.MinValue =  1;
      mTemplateCount.MaxValue = 50;
      mTemplates = new List<StringValueObject>();
      ListHelpers.ConnectListToCounter(mTemplates, mTemplateCount,
          mTypeService.GetValueObjectCreator(PortTypes.String, TEMPLATE_PREFIX + " "),
          updateTemplate);

      // Initialize token object storage and output per template parameter
      mTokensPerTemplate = new List<List<TokenBase>>();
      mOutputs = new List<IValueObject>();
      updateTemplateCount();
      mTemplateCount.ValueSet += updateTemplateCount;

      // Initialize the input counts
      mBinInputCount = mTypeService.CreateInt(PortTypes.Integer, INPUT_BIN_PREFIX+"Count", 0);
      mIntInputCount = mTypeService.CreateInt(PortTypes.Integer, INPUT_INT_PREFIX+"Count", 0);
      mNumInputCount = mTypeService.CreateInt(PortTypes.Integer, INPUT_NUM_PREFIX+"Count", 0);
      mStrInputCount = mTypeService.CreateInt(PortTypes.Integer, INPUT_STR_PREFIX+"Count", 0);

      // Initialize inputs using helper functions that grow/shrink the list of
      // inputs whenever the input count is changed
      mBinInputs = new List<BoolValueObject>();
      ListHelpers.ConnectListToCounter(mBinInputs, mBinInputCount,
          mTypeService.GetValueObjectCreator(PortTypes.Bool, INPUT_BIN_PREFIX),
          updateOutputValues);
      mIntInputs = new List<IntValueObject>();
      ListHelpers.ConnectListToCounter(mIntInputs, mIntInputCount,
          mTypeService.GetValueObjectCreator(PortTypes.Integer, INPUT_INT_PREFIX),
          updateOutputValues);
      mNumInputs = new List<DoubleValueObject>();
      ListHelpers.ConnectListToCounter(mNumInputs, mNumInputCount,
          mTypeService.GetValueObjectCreator(PortTypes.Number, INPUT_NUM_PREFIX),
          updateOutputValues);
      mStrInputs = new List<StringValueObject>();
      ListHelpers.ConnectListToCounter(mStrInputs, mStrInputCount,
          mTypeService.GetValueObjectCreator(PortTypes.String, INPUT_STR_PREFIX),
          updateOutputValues);
    }

    /// <summary>
    /// Template string with placeholders to merge input values into.
    /// </summary>
    [Parameter(DisplayOrder =  1, InitOrder = 1, IsDefaultShown = false)]
    public IntValueObject mTemplateCount { get; private set; }
    [Parameter(DisplayOrder = 20, InitOrder = 2, IsDefaultShown = true)]
    public IList<StringValueObject> mTemplates { get; private set; }
    // Parsed, ready-to-evaluate placeholder tokens for each of the templates
    // public only for testability
    public List<List<TokenBase>> mTokensPerTemplate { get; private set; }

    /// <summary>
    /// Token indices, counts, and lists of value inputs. 
    /// </summary>
    public IntValueObject mBinInputCount { get; private set; }
    public IntValueObject mIntInputCount { get; private set; }
    public IntValueObject mNumInputCount { get; private set; }
    public IntValueObject mStrInputCount { get; private set; }
    [Input(DisplayOrder = 30, InitOrder = 30, IsDefaultShown = true)]
    public IList<BoolValueObject> mBinInputs { get; private set; }
    [Input(DisplayOrder = 31, InitOrder = 33, IsDefaultShown = true)]
    public IList<IntValueObject> mIntInputs { get; private set; }
    [Input(DisplayOrder = 32, InitOrder = 33, IsDefaultShown = true)]
    public IList<DoubleValueObject> mNumInputs { get; private set; }
    [Input(DisplayOrder = 33, InitOrder = 33, IsDefaultShown = true)]
    public IList<StringValueObject> mStrInputs { get; private set; }

    /// <summary>
    /// The value outputs
    /// </summary>
    [Output(DisplayOrder = 1, IsRequired = true)]
    public List<IValueObject> mOutputs { get; set; }

    /// <summary>
    /// This method has been added as a ValueSet handler to the template
    /// count parameter. It will therefore be called whenever the number
    /// of template strings is changed, in order to update the list of
    /// token lists, list of outputs, and other members that derived
    /// classes may need.
    /// </summary>
    protected void updateTemplateCount(object sender = null,
                        ValueChangedEventArgs evArgs = null)
    {
      int newCount = mTemplateCount.Value;
      int oldCount = mOutputs.Count;

      // Truncate member lists if they are too long
      if ( newCount < oldCount )
      {
        mTokensPerTemplate.RemoveRange(newCount, oldCount - newCount);
        mOutputs.RemoveRange(newCount, oldCount - newCount);
      }
      else
      {
        for (int i = oldCount; i < newCount; i++)
        {
          mTokensPerTemplate.Add(new List<TokenBase>(10));
          mOutputs.Add(createOutput(i));
        }
      }

      // Update output related mebers of derived classes
      updateOutputHelpers(newCount, oldCount);
    }

    protected abstract IValueObject createOutput(int i);

    protected string getOutputName(int i)
    {
      return OUTPUT_PREFIX + " " + (i+1).ToString();
    }

    protected abstract void updateOutputHelpers(int newCount, int oldCount);

    /// <summary>
    /// This method has been added as a ValueSet handler to the template
    /// parameters. It will therefore be called whenever a template string
    /// is edited, in order to update the tokens and inputs.
    /// </summary>
    private void updateTemplate(object sender = null,
                 ValueChangedEventArgs evArgs = null)
    {
      // Validate the separators first to prevent throwing exceptions later
      // in System.Globalization
      ValidationResult result = validateSeparators("en");
      if (result.HasError)
      {
        return;
      }

      // Even though only one template has changed we must re-evaluate all
      // templates, because these depend on each other due to placeholder
      // re -use.
      TemplateTokenFactory ttf = TemplateTokenFactory.Instance;
      ttf.restart();

      int binCount = 0;
      int intCount = 0;
      int numCount = 0;
      int strCount = 0;

      List<TokenBase> allTokens = new List<TokenBase>(100);

      for (int i = 0; i < mTemplates.Count; i++)
      {
        // Check parameter.
        // Along the way fill mTemplateTokens and count numbers of inputs
        List<TokenBase> localTokens = new List<TokenBase>(10);
        ValidationResult res = validateInternal(mTemplates[i], ref localTokens,
                    ref binCount, ref intCount, ref numCount, ref strCount, "en");
        if (res.HasError)
        {
          return;
        }
        mTokensPerTemplate[i] = localTokens;
        allTokens.AddRange(localTokens);
      }

      // Update the input counts if successful
      mBinInputCount.Value = binCount;
      mIntInputCount.Value = intCount;
      mNumInputCount.Value = numCount;
      mStrInputCount.Value = strCount;

      int binIdx = 0;
      int intIdx = 0;
      int numIdx = 0;
      int strIdx = 0;

      foreach (TokenBase token in allTokens)
      {
        if (token is VarTokenBase varToken)
        {
          switch (varToken.getType())
          {
            case TokenType.VarBoolean:
              mBinInputs[binIdx].Name = varToken.getName();
              varToken.setInput(mBinInputs[binIdx]);
              binIdx++;
              break;
            case TokenType.VarInteger:
              mIntInputs[intIdx].Name = varToken.getName();
              varToken.setInput(mIntInputs[intIdx]);
              intIdx++;
              break;
            case TokenType.VarNumber:
              mNumInputs[numIdx].Name = varToken.getName();
              varToken.setInput(mNumInputs[numIdx]);
              numIdx++;
              break;
            case TokenType.VarString:
              mStrInputs[strIdx].Name = varToken.getName();
              varToken.setInput(mStrInputs[strIdx]);
              strIdx++;
              break;
            default:
              // Nothing to do
              break;
          }
        }
      }
      updateTemplateHelpers();
    }

    protected abstract void updateTemplateHelpers();

    /// <summary>
    /// Called when the logic sheets are checked for correctness, in order to check
    /// this node's correct configuration.
    /// </summary>
    /// <param name="language">
    /// The language key which is used for localizing the validation message.
    /// </param>
    /// 
    public override sealed ValidationResult Validate(string language)
    {
      ValidationResult result = validateSeparators(language);
      if (result.HasError)
      {
        return result;
      }

      TemplateTokenFactory ttf = TemplateTokenFactory.Instance;
      ttf.restart();

      foreach (var template in mTemplates)
      {
        int binCountDummy = 0;
        int intCountDummy = 0;
        int numCountDummy = 0;
        int strCountDummy = 0;
        List<TokenBase> localTokens = new List<TokenBase>(10);
        result = validateInternal(template, ref localTokens,
                                  ref binCountDummy, ref intCountDummy,
                                  ref numCountDummy, ref strCountDummy, language);
        if (result.HasError)
        {
          return result;
        }
      }
      return base.Validate(language);
    }

    private ValidationResult validateSeparators(string language)
    {
      // Validate separators
      if (getGroupSeparator().Length > 1)
      {
        return new ValidationResult
        {
          HasError = true,
          Message = Localize(language, "SeparatorGroupTooLong")
        };
      }
      if (getDecimalSeparator().Length > 1)
      {
        return new ValidationResult
        {
          HasError = true,
          Message = Localize(language, "SeparatorDecimalTooLong")
        };
      }
      if (getDecimalSeparator().Length < 1)
      {
        return new ValidationResult
        {
          HasError = true,
          Message = Localize(language, "SeparatorDecimalTooShort")
        };
      }
      if (getDecimalSeparator() == getGroupSeparator())
      {
        return new ValidationResult
        {
          HasError = true,
          Message = Localize(language, "SeparatorsIdentical")
        };
      }
      return new ValidationResult { HasError = false };
    }

    private ValidationResult validateInternal(StringValueObject template,
                                            ref List<TokenBase> templateTokens,
                                                        ref int binCount,
                                                        ref int intCount,
                                                        ref int numCount,
                                                        ref int strCount,
                                                         string language)
    {
      if ( (!template.HasValue) || (template.Value.Length <= 0))
      {
        return new ValidationResult
        {
          HasError = true,
          Message = Localize(language, "EmptyTemplate")
        };
      }

      TemplateTokenFactory ttf = TemplateTokenFactory.Instance;

      for (int curPos = 0; curPos < template.Value.Length;)
      {
        Match curMatch = PLACEHOLDER_REGEX.Match(template.Value, curPos);
        if (curMatch.Success && (curMatch.Length >= 2))
        {
          // constant string before placeholder
          templateTokens.Add(ttf.createConstStringToken(
            template.Value.Substring(curPos, curMatch.Index - curPos)));
          // placeholder string between delimiters
          templateTokens.Add(
            ttf.createPlaceholderToken(
              template.Value.Substring(curMatch.Index + 1, curMatch.Length - 2),
              getGroupSeparator(), getDecimalSeparator()
            )
          );
          curPos = curMatch.Index + curMatch.Length;
        }
        else
        {
          // constant string after last placeholder
          templateTokens.Add(ttf.createConstStringToken(
            template.Value.Substring(curPos)));

          curPos = template.Value.Length;
        }
      }
      ValidationResult result = validateNumberOfTokens(language, ref templateTokens);
      if (result.HasError)
      {
        return result;
      }

      foreach (TokenBase token in templateTokens)
      {
        switch (token.getType())
        {
          case TokenType.ConstString:
          case TokenType.VarReference:
            // nothing to do
            break;
          case TokenType.VarBoolean:
            binCount++;
            break;
          case TokenType.VarInteger:
            intCount++;
            break;
          case TokenType.VarNumber:
            numCount++;
            break;
          case TokenType.VarString:
            strCount++;
            break;
          case TokenType.Error:
          default:
            return createTokenError(language, template.Name, token);
        }
      }
      if (binCount > MAX_INPUTS)
      {
        return new ValidationResult
        {
          HasError = true,
          Message = Localize(language, "TooManyBinPlaceholders")
        };
      }
      if (intCount > MAX_INPUTS)
      {
        return new ValidationResult
        {
          HasError = true,
          Message = Localize(language, "TooManyIntPlaceholders")
        };
      }
      if (numCount > MAX_INPUTS)
      {
        return new ValidationResult
        {
          HasError = true,
          Message = Localize(language, "TooManyNumPlaceholders")
        };
      }
      if (strCount > MAX_INPUTS)
      {
        return new ValidationResult
        {
          HasError = true,
          Message = Localize(language, "TooManyStrPlaceholders")
        };
      }
      return validateTokens(language, template.Name, ref templateTokens);
    }

    protected ValidationResult createTokenError(string language,
                                                string templateName,
                                             TokenBase token)
    {
      return new ValidationResult {
        HasError = true,
        Message = Localize(language, templateName) + ": " +
                  Char.ToString(PLACEHOLDER_DELIMITERS[0]) +
                  token.getSource() +
                  Char.ToString(PLACEHOLDER_DELIMITERS[1]) +
                  Localize(language, token.getError())
      };
    }

    protected ValidationResult createTextError(string language,
                                                string templateName,
                                             TokenBase token)
    {
      return new ValidationResult {
        HasError = true,
        Message = Localize(language, templateName) + ": " +
                  "…" + token.getSource() + "…" +
                  Localize(language, token.getError())
      };
    }

    protected abstract ValidationResult validateTokens(string language,
                                                       string templateName,
                                          ref List<TokenBase> templateTokens);

    private ValidationResult validateNumberOfTokens(string language,
                                          ref List<TokenBase> templateTokens)
    {
      if (templateTokens.Count < (isPlaceholderEnforced() ? 2 : 1))
      {
        return new ValidationResult
        {
          HasError = true,
          Message = Localize(language, "NoPlaceholder")
        };
      }
      if (templateTokens.Count > (6 * MAX_INPUTS + 1))
      {
        return new ValidationResult
        {
          HasError = true,
          Message = Localize(language, "TooManyPlaceholders")
        };
      }
      return new ValidationResult { HasError = false };
    }

    protected virtual bool isPlaceholderEnforced()
    {
      return true;
    }

    /// <summary>
    /// This method has been added as a ValueSet handler to each input. It will
    /// therefore be called when ANY of the inputs receives a value. Derived
    /// classes can implement this empty, and use Execute to update their
    /// outputs then usual way. Alternatively they can implement this method to
    /// update outputs even before all inputs have received values.
    /// </summary>
    protected abstract void updateOutputValues(object sender = null,
                          ValueChangedEventArgs evArgs = null);

    /// <summary>
    /// This method is called by the framework to localize strings. We override
    /// the base class implementation to allow localization of our autmatically
    /// created input names.
    /// </summary>
    public override string Localize(string language, string key)
    {
      string templateCount = TEMPLATE_PREFIX + "Count";
      if (key.Equals(templateCount))
      {
        return base.Localize(language, templateCount);
      }
      if (key.StartsWith(TEMPLATE_PREFIX))
      {
        string suffix = key.Substring(TEMPLATE_PREFIX.Length);
        return base.Localize(language, TEMPLATE_PREFIX) + suffix;
      }
      if (key.StartsWith(OUTPUT_PREFIX))
      {
        string suffix = key.Substring(OUTPUT_PREFIX.Length);
        return base.Localize(language, OUTPUT_PREFIX) + suffix;
      }
      if (key.StartsWith(INPUT_BIN_PREFIX))
      {
        string suffix = key.Substring(INPUT_BIN_PREFIX.Length);
        return base.Localize(language, INPUT_BIN_PREFIX) + suffix;
      }
      if (key.StartsWith(INPUT_INT_PREFIX))
      {
        string suffix = key.Substring(INPUT_INT_PREFIX.Length);
        return base.Localize(language, INPUT_INT_PREFIX) + suffix;
      }
      if (key.StartsWith(INPUT_NUM_PREFIX))
      {
        string suffix = key.Substring(INPUT_NUM_PREFIX.Length);
        return base.Localize(language, INPUT_NUM_PREFIX) + suffix;
      }
      if (key.StartsWith(INPUT_STR_PREFIX))
      {
        string suffix = key.Substring(INPUT_STR_PREFIX.Length);
        return base.Localize(language, INPUT_STR_PREFIX) + suffix;
      }
      return base.Localize(language, key);
    }
  }
}
