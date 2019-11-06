using System;
using System.IO;
using System.Text;
using System.Globalization;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using LogicModule.Nodes.Helpers;
using LogicModule.ObjectModel;
using LogicModule.ObjectModel.TypeSystem;
using System.Reflection;
using FieldDictionary = System.Collections.Generic.Dictionary<
  string, System.Tuple<Mono.CSharp.FieldSpec, System.Reflection.FieldInfo>>;
using Mono.CSharp;

namespace Recomedia_de.Logic.VisuWeb
{
  /// <summary>
  /// Calculates a mathematical formula given as text and an arbitrary
  /// number of value inputs. Inputs are created as needed based on
  /// placeholders in the formula string.
  /// </summary>
  public class ExpressionCalculator : PlaceholderNodeBase
  {
    protected override string TEMPLATE_PREFIX { get { return "Expression"; } }
    private const string OUT_TYPE_PREFIX = "OutType";

    /// <summary>
    /// The expression evaluation engine its compiled output, and variable
    /// fields.
    /// </summary>
    private StringBuilder mEngineReport;
    private Evaluator mEngine;
    private List<CompiledMethod> mCompiled;
    private FieldDictionary mFields;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExpressionCalculator"/> class.
    /// </summary>
    /// <param name="context">The node context.</param>
    public ExpressionCalculator(INodeContext context)
      : base(context)
    {
      // The list of output data type enums is initialized in updateOutputHelpers

      // Initialize error text output
      mError = mTypeService.CreateString(PortTypes.String, "RuntimeError");
    }

    /// <summary>
    /// The types of each output
    /// </summary>
    [Parameter(DisplayOrder = 10, InitOrder = 10, IsDefaultShown = false)]
    public List<EnumValueObject> mOutputTypes { get; private set; }
    private readonly string[] mTypeAllowedValues = { PortTypes.Number,
        PortTypes.Int64, PortTypes.Integer, PortTypes.Byte, PortTypes.Bool, PortTypes.String };

    /// <summary>
    /// The error output.
    /// </summary>
    [Output(DisplayOrder = 9, IsRequired = true)]
    public StringValueObject mError { get; private set; }

    protected override IValueObject createOutput(int i)
    {
      // New outputs always start off with their default type.
      // They can later be updated to a new type in updateOutputType()
      return mTypeService.CreateDouble(mTypeAllowedValues[0], getOutputName(i));
    }

    protected override void updateOutputHelpers(int newCount, int oldCount)
    {
      if ( null == mOutputTypes )
      {
        mOutputTypes = new List<EnumValueObject>();
      }

      // Truncate member lists if they are too long
      if (newCount < oldCount)
      {
        mOutputTypes.RemoveRange(newCount, oldCount - newCount);
      }
      else
      {
        for (int i = oldCount; i < newCount; i++)
        {
          var newOutType = mTypeService.CreateEnum("EOutputType",
                      OUT_TYPE_PREFIX + " " + (i + 1).ToString(),
                         mTypeAllowedValues, mTypeAllowedValues[0]);
          newOutType.ValueSet += updateOutputType;
          mOutputTypes.Add(newOutType);
        }
      }
      exitEvalEngine();
    }

    protected override void updateTemplateHelpers()
    {
      exitEvalEngine();
    }


    /// <summary>
    /// Called when an output type receives a new value, in order to update its
    /// output accordingly.
    /// </summary>
    private void updateOutputType(object sender,
                   ValueChangedEventArgs evArgs = null)
    {
      if (sender is EnumValueObject outputType)
      {
        int i = getTemplateIndex(outputType);
        if (i >= 0)
        {
          if (PortTypes.Number == outputType.Value)
          {
            mOutputs[i] = mTypeService.CreateDouble(PortTypes.Number, getOutputName(i));
          }
          else if (PortTypes.Int64 == outputType.Value)
          {
            mOutputs[i] = mTypeService.CreateValueObject(PortTypes.Int64, getOutputName(i));
          }
          else if (PortTypes.Integer == outputType.Value)
          {
            mOutputs[i] = mTypeService.CreateInt(PortTypes.Integer, getOutputName(i));
          }
          else if (PortTypes.Byte == outputType.Value)
          {
            mOutputs[i] = mTypeService.CreateByte(PortTypes.Byte, getOutputName(i));
          }
          else if (PortTypes.Bool == outputType.Value)
          {
            mOutputs[i] = mTypeService.CreateBool(PortTypes.Bool, getOutputName(i));
          }
          else if (PortTypes.String == outputType.Value)
          {
            mOutputs[i] = mTypeService.CreateString(PortTypes.String, getOutputName(i));
          }
        }
      }
    }

    /// <summary>
    /// Find the template index that corresponds to the given output type value object
    /// </summary>
    private int getTemplateIndex(EnumValueObject outputType)
    {
      for (int i = 0; i < mOutputTypes.Count; i++)
      {
        if ( mOutputTypes[i] == outputType)
        {
          return i;
        }
      }
      return -1;
    }

    /// <summary>
    /// Verify that tokens have no format specification and that they have a
    /// user-supplied, valid identifier as name. Offending tokens are replaced
    /// by error tokens. Everything else we leav to the interpreter to check
    /// in Execute().
    /// </summary>
    protected override ValidationResult validateTokens(string language,
                                                       string templateName,
                                          ref List<TokenBase> templateTokens)
    {
      for ( int i = 0; i < templateTokens.Count; i++ )
      {
        TokenBase token = templateTokens[i];

        if ( token.hasFormatOrMappings() )
        {
          templateTokens[i] = new ErrorToken(token.getSource(),
                                             "HasFormatOrMappings");
          return createTokenError(language, templateName, templateTokens[i]);
        }
        if ( token.hasDefaultName() )
        {
          templateTokens[i] = new ErrorToken(token.getSource(),
                                             "HasDefaultName");
          return createTokenError(language, templateName, templateTokens[i]);
        }
        if (token is ConstStringToken csToken)
        {
          string text = csToken.getText();
          if (hasOutOfRangeRef(text, templateName))
          {
            templateTokens[i] = new ErrorToken(token.getSource(),
                                               "HasOutOfRangeRef");
            return createTextError(language, templateName, templateTokens[i]);
          }
          if (hasAssignment(text))
          {
            templateTokens[i] = new ErrorToken(token.getSource(),
                                               "HasAssignment");
            return createTextError(language, templateName, templateTokens[i]);
          }
        }
        if ( token is VarTokenBase varToken )
        {
          if ( !isIdentifier(varToken.getName()) )
          {
            templateTokens[i] = new ErrorToken(token.getSource(),
                                               "HasUnusableName");
            return createTokenError(language, templateName, templateTokens[i]);
          }
        }
      }
      return new ValidationResult { HasError = false };
    }

    protected override bool isPlaceholderEnforced()
    {
      return false;
    }

    private bool hasOutOfRangeRef(string text, string templateName)
    {
      int exprNum = int.Parse(templateName.Substring(TEMPLATE_PREFIX.Length));

      foreach (Match match in Regex.Matches(text, @"_out([0-9]*)_"))
      {
        int number = int.Parse(match.Groups[1].Value);
        if ((number <= 0) || (number >= exprNum))
        {
          return true;
        }
      }
      return false;
    }

    private bool hasAssignment(string text)
    {
      int maxI = text.Length - 1;
      if (maxI < 0)
      {
        return false;   // empty
      }
      int i = 0;
      while ((i = text.IndexOf('=', i)) != -1)
      {
        if (i > 0)
        {
          switch (text[i - 1])
          {
            case '=':
            case '!':
            case '>':
            case '<':
              i++;
              continue;
            default:
              break;
          }
        }
        if (i < maxI)
        {
          switch (text[i + 1])
          {
            case '=':
              i++;
              continue;
            default:
              return true;
          }
        }
        return true;
      }
      return false;
    }

    public static bool isIdentifier(string text)
    {
      if (string.IsNullOrEmpty(text))
        return false;
      if (!char.IsLetter(text[0]) && text[0] != '_')
        return false;
      for (int ix = 1; ix < text.Length; ++ix)
        if (!char.IsLetterOrDigit(text[ix]) && text[ix] != '_')
          return false;
      return true;
    }

    /// <summary>
    /// This method has been added as a ValueSet handler to each input. It will
    /// therefore be called when ANY of the inputs receives a value, in order to
    /// update the output. We don't use this, because we cannot update outputs
    /// without a complete set of input values. We therefore use the Execute
    /// method.
    /// </summary>
    protected override void updateOutputValues(object sender = null,
               ValueChangedEventArgs evArgs = null)
    {
      // Nothing to do before all inputs have values; see Execute
    }

    /// <summary>
    /// Called when an input receives a new value and all inputs have values.
    /// </summary>
    public override void Execute()
    {
      mError.Value = "";
      initEvalEngine();
      updateVariableFields();
      for (int i = 0; i < mTokensPerTemplate.Count; i++)
      {
        try
        {
          if (mCompiled[i] == null)
          { // Compile only once
            string expressionText = getExpressionText(i);
            if (expressionText.Length > 0)
            {
              mCompiled[i] = mEngine.Compile(expressionText + ";");
            }
          }
          if (mCompiled[i] != null)
          {
            // Need a temporary object of appropriate type to store output in
            object value = getValueOfOutputType(i);
            // Evaluate pre-compiled expression and store in final output
            mCompiled[i](ref value);
            mOutputs[i].Value = value;
            updateOutputField(i);
          }
          else
          {
            // Issue compile error report on error output
            mError.Value += mTemplates[i].Name + ": " +
                            mEngineReport.ToString();
          }
        }
        catch (Exception ex)
        {
          // Issue first line of exception message and error report
          mError.Value += mTemplates[i].Name + ": " +
                          ex.Message + Environment.NewLine +
                          mEngineReport.ToString();
        }
      }
    }

    private void exitEvalEngine()
    {
      if (mEngine != null)
      {
        // Initialize the expression evaluation engine
        mCompiled = null;
        mFields = null;
        mEngine = null;
        mEngineReport = null;
      }
    }

    private void initEvalEngine()
    {
      if (mEngine == null)
      {
        // Initialize the expression evaluator
        mEngineReport = new StringBuilder();
        var writer = new StringWriter(mEngineReport);
        var printer = new StreamReportPrinter(writer);
        var evalSettings = new CompilerSettings();
        evalSettings.AssemblyReferences.Add("Recomedia_de.Logic.VisuWeb.dll");
        var evalContext = new CompilerContext(evalSettings, new StreamReportPrinter(writer));
        mEngine = new Evaluator(evalContext);

        // Using supported namespaces
        mEngine.Run("using System;" + Environment.NewLine +
                    "using Recomedia_de.Logic.VisuWeb;");

        // Declare all variables we will ever use
        string declText = getVariableDecls();
        mEngine.Run(declText);

        // After we declared all needed variables is the time to get access to
        // them for later field updates
        const BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance;
        FieldInfo fieldInfo = typeof(Evaluator).GetField("fields", bindingFlags);
        mFields = fieldInfo.GetValue(mEngine) as FieldDictionary;

        // Start with nulled compiled expressions (lazy-init in Execute)
        mCompiled = new List<CompiledMethod>(
                            Enumerable.Repeat<CompiledMethod>(null, mOutputs.Count));
      }
    }

    private string getVariableDecls()
    {
      var declText = "";
      foreach (var boolInput in mBinInputs)
      {
        declText += getVariableDecl(boolInput);
      }
      foreach (var intInput in mIntInputs)
      {
        declText += getVariableDecl(intInput);
      }
      foreach (var doubleInput in mNumInputs)
      {
        declText += getVariableDecl(doubleInput);
      }
      foreach (var stringInput in mStrInputs)
      {
        declText += getVariableDecl(stringInput);
      }
      for (int i = 0; i < mOutputs.Count; i++)
      {
        declText += getOutputDecl(i);
      }
      return declText;
    }

    private string getVariableDecl(ValueObjectBase input)
    {
      string typeStr = getTypeString(input.PortType);
      string initStr = input.HasValue ? getInitString(input.PortType, input.Value) : "";
      input.WasSet = false; // Value has been used
      return typeStr + " " + input.Name + initStr + "; ";
    }

    private string getOutputDecl(int i)
    {
      string typeStr = getTypeString(mOutputs[i].PortType);
      return typeStr + " " + createOutIdentifier(i) + "; ";
    }

    private string createOutIdentifier(int i)
    {
      string outIdentifier = "_out" + (i + 1).ToString() + "_";
      return outIdentifier;
    }

    private void updateVariableFields()
    {
      foreach (var boolInput in mBinInputs)
      {
        updateVariableField(boolInput);
      }
      foreach (var intInput in mIntInputs)
      {
        updateVariableField(intInput);
      }
      foreach (var doubleInput in mNumInputs)
      {
        updateVariableField(doubleInput);
      }
      foreach (var stringInput in mStrInputs)
      {
        updateVariableField(stringInput);
      }
    }

    private void updateVariableField(ValueObjectBase var)
    {
      if ( var.WasSet )
      {
        updateField(var.Name, var.Value);
      }
    }

    private void updateOutputField(int i)
    {
      updateField(createOutIdentifier(i), mOutputs[i].Value);
    }

    private void updateField(string name, object value)
    {
      Tuple<FieldSpec, FieldInfo> field;
      bool found = mFields.TryGetValue(name, out field);
      if (found && (field != null))
      {
        field.Item2.SetValue(mEngine, value);
      }
    }

    private string getExpressionText(int i)
    {
      string expressionText = "";
      foreach (TokenBase token in mTokensPerTemplate[i])
      {
        if (token is VarTokenBase varToken)
        {
          expressionText += varToken.getName();
        }
        else if (token is ConstStringToken csToken)
        {
          expressionText += csToken.getText();
        }
        else
        {
          mError.Value += mTemplates[i].Name + ": Invalid token in Execute()" +
                                                           Environment.NewLine;
          return "";
        }
      }
      if (expressionText.Length == 0)
      {
        mError.Value += mTemplates[i].Name + ": Empty expression in Execute()" +
                                                         Environment.NewLine;
        return "";
      }
      return expressionText;
    }

    private object getValueOfOutputType(int i)
    {
      string outputType = mOutputs[i].PortType.Name;

      if (PortTypes.Number == outputType)
      {
        return new double();
      }
      else if (PortTypes.Int64 == outputType)
      {
        return new Int64();
      }
      else if (PortTypes.Integer == outputType)
      {
        return new int();
      }
      else if (PortTypes.Byte == outputType)
      {
        return new byte();
      }
      else if (PortTypes.Bool == outputType)
      {
        return new bool();
      }
      else if (PortTypes.String == outputType)
      {
        return String.Empty.Clone();
      }
      return null;
    }

    private string getTypeString(IPortType type)
    { 
      string typeStr = type.Name;
      switch (typeStr)
      {
        case PortTypes.Number:
          typeStr = "double";
          break;
        case PortTypes.Int64:
          typeStr = "long";
          break;
        case PortTypes.Integer:
          typeStr = "int";
          break;
        default:  // byte, bool, or string
          typeStr = type.Name.ToLower();
          break;
      }
      return typeStr;
    }

    private string getInitString(IPortType type, object value)
    {
      string initStr = " = ";
      switch (type.Name)
      {
        case PortTypes.Number:
          if (value is Double numValue)
          {
            initStr += numValue.ToString(CultureInfo.InvariantCulture);
          }
          else
          {
            initStr += "Double.NaN";
          }
          break;
        case PortTypes.String:
          initStr += "\"" + value.ToString() + "\"";
          break;
        default:  // all kinds of integer values
          initStr += value.ToString().ToLower();
          break;
      }
      return initStr;
    }

    /// <summary>
    /// This method is called by the framework to localize strings. We override
    /// the base class implementation to allow localization of our autmatically
    /// created input names.
    /// </summary>
    public override string Localize(string language, string key)
    {
      string templateCountPrefix = TEMPLATE_PREFIX + "Count";
      if (key.Equals(templateCountPrefix))
      {
        return base.Localize(language, templateCountPrefix);
      }
      if (key.StartsWith(OUT_TYPE_PREFIX))
      {
        string suffix = key.Substring(OUT_TYPE_PREFIX.Length);
        return base.Localize(language, OUT_TYPE_PREFIX) + suffix;
      }
      return base.Localize(language, key);
    }
  }

}
