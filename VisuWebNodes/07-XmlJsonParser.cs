using System.Collections.Generic;
using System.Globalization;
using System;
using System.IO;
using System.Xml;
using System.Runtime.Serialization.Json;

using LogicModule.Nodes.Helpers;
using LogicModule.ObjectModel;
using LogicModule.ObjectModel.TypeSystem;

namespace Recomedia_de.Logic.VisuWeb
{
  /// <summary>
  /// Pass the input to one out of many outputs.
  /// </summary>
  /// <remarks>
  /// When the input selection changes, an "idle" value can be sent to the de-
  /// selected output, and/or the last value sent before de-selection can be
  /// sent to the selected output.
  /// </remarks>
  public class XmlJsonParser : LocalizablePrefixLogicNodeBase
  {
    /// <summary>
    /// The operations to select one or more nodes and postprocess them.
    /// </summary>
    private const string OPERATION_1STASTEXT = "FirstAsText";
    private const string OPERATION_1STASNUMBER = "FirstAsNumber";
    private const string OPERATION_CONCATTEXTS = "MultiConcatTexts";
    private const string OPERATION_ADDNUMBERS = "MultiAddNumbers";
    private const string PARAM_POSTFIX = "Param";
    private const string PARAM_EMPTY = "";

    private const string INPUT_NAME = "Input";

    /// <summary>
    /// The prefixes to use for the names of the parameters and outputs. An index is
    /// added to give a unique name.
    /// </summary>
    private const string OUTPUT_PREFIX = "Output";
    private const string PATH_PREFIX = "Path";
    private const string OPERATION_PREFIX = "SelectOperation";
    private const string SEL_OP_PARAM_PREFIX = "SelOperParam";

    /// <summary>
    /// Parse numbers in the GUI using comma or point as the decimal separator
    /// </summary>
    private readonly CultureInfo mCommaCulture;
    private readonly CultureInfo mPointCulture;

    /// <summary>
    /// The TypeService is saved to allow updating after the constructor is finished.
    /// </summary>
    protected readonly ITypeService mTypeService;

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    /// <param name="context">The node context.</param>
    public XmlJsonParser(INodeContext context)
      : base(context, OUTPUT_PREFIX)
    {
      context.ThrowIfNull("context");
      mTypeService = context.GetService<ITypeService>();

      // Initialize the input.
      mInput = mTypeService.CreateString(PortTypes.String, INPUT_NAME);

      // Initialize the selector for "XML" or "JSON".
      mSelectInput = mTypeService.CreateEnum("ESelectCode", "SelectCode", mSelectInputValues, "XML");

      // Initialize the output etc. parameter count.
      mCount = mTypeService.CreateInt(PortTypes.Integer, "OutputCount", 1);
      mCount.MinValue = 1;
      mCount.MaxValue = 50;
      mCount.ValueSet += updateSelectOperationCount;

      // Initialize operation selector parameters
      mSelectOperation = new List<EnumValueObject>();
      updateSelectOperationCount();

      // Initialize operation parameters using a helper function that grows/shrinks
      // the list whenever the count is changed
      mSelectParam = new List<StringValueObject>();
      ListHelpers.ConnectListToCounter(mSelectParam, mCount,
          mTypeService.GetValueObjectCreator(PortTypes.String, SEL_OP_PARAM_PREFIX),
          null, updateSelectParamDefault);
      updateSelectParamDefault();

      // Initialize path parameters using a helper function that grows/shrinks the
      // list whenever the count is changed
      mPath = new List<StringValueObject>();
      ListHelpers.ConnectListToCounter(mPath, mCount,
          mTypeService.GetValueObjectCreator(PortTypes.String, PATH_PREFIX), null);

      // Initialize outputs using a helper function that grows/shrinks the list of
      // outputs whenever the output count is changed
      mOutput = new List<AnyValueObject>();
      ListHelpers.ConnectListToCounter(mOutput, mCount,
          mTypeService.GetValueObjectCreator(PortTypes.Any, OUTPUT_PREFIX), null);

      // Initialize error text output
      mError = mTypeService.CreateString(PortTypes.String, "RuntimeError");

      mCommaCulture = new CultureInfo("de");
      mPointCulture = new CultureInfo("en");
    }

    /// <summary>
    /// The input to pass to the selected output.
    /// </summary>
    [Input(DisplayOrder = 2)]
    public StringValueObject mInput { get; private set; }

    /// <summary>
    /// What kind of input to parse.
    /// </summary>
    [Parameter(DisplayOrder = 1, InitOrder = 2, IsDefaultShown = false)]
    public EnumValueObject mSelectInput { get; private set; }
    private readonly string[] mSelectInputValues = { "XML", "JSON" };

    /// <summary>
    /// How many paths, outputs, etc. shall be used.
    /// </summary>
    [Parameter(DisplayOrder = 3, InitOrder = 1, IsDefaultShown = false)]
    public IntValueObject mCount { get; private set; }

    /// <summary>
    /// Which item(s) to select and send to the corresponding output, as an XPath selector.
    /// </summary>
    [Parameter(DisplayOrder = 4, InitOrder = 3, IsDefaultShown = true)]
    public IList<StringValueObject> mPath { get; private set; }

    /// <summary>
    /// How many items to select and how forward their value(s) to the output.
    /// </summary>
    [Parameter(DisplayOrder = 5, InitOrder = 3, IsDefaultShown = false)]
    public List<EnumValueObject> mSelectOperation { get; private set; }
    static private readonly string[] mSelectOperationValues = {
                                OPERATION_1STASTEXT, OPERATION_1STASNUMBER,
                                OPERATION_CONCATTEXTS, OPERATION_ADDNUMBERS };

    /// <summary>
    /// Parameter semantics depend on selected kind of processing:
    /// - First as text: Output text prefix
    /// - First as number: Scaling factor
    /// - All texts: Text to insert between concatenated items
    /// - Added numbers: Scaling factor for sum of all found values
    /// </summary>
    [Parameter(DisplayOrder = 6, InitOrder = 3, IsDefaultShown = false)]
    public IList<StringValueObject> mSelectParam { get; private set; }

    /// <summary>
    /// The error output.
    /// </summary>
    [Output(DisplayOrder = 1, IsDefaultShown = true, IsRequired = true)]
    public StringValueObject mError { get; private set; }
    private string mLanguage = "de";

    /// <summary>
    /// A list of output values. 
    /// </summary>
    [Output(DisplayOrder = 2, IsDefaultShown = true, IsRequired = true)]
    public IList<AnyValueObject> mOutput { get; private set; }

    /// <summary>
    /// This method has been added as a ValueSet handler to the count parameter.
    /// It will therefore be called whenever the number of outputs (and operations)
    /// is changed, in order to update the list of operations.
    /// </summary>
    private void updateSelectOperationCount(object sender = null,
                    ValueChangedEventArgs evArgs = null)
    {
      int newCount = mCount.Value;
      int oldCount = mSelectOperation.Count;

      // Truncate member lists if they are too long
      if (newCount < oldCount)
      {
        mSelectOperation.RemoveRange(newCount, oldCount - newCount);
      }
      else
      {
        for (int i = oldCount; i < newCount; i++)
        {
          var e = mTypeService.CreateEnum("ESelectOperation",
                       OPERATION_PREFIX + (i+1).ToString(),
                       mSelectOperationValues, OPERATION_1STASTEXT);
          mSelectOperation.Add(e);
        }
      }
    }

    /// <summary>
    /// This method has been added as a ValueSet handler to the SelectParam
    /// parameters. It will therefore be called whenever any of the params is
    /// changed, in order to update empty values to the empty special value.
    /// </summary>
    private void updateSelectParamDefault()
    {
      for (int i = 0; i < mSelectParam.Count; i++)
      {
        if (getSelectParam(i).Length <= 0)
        {
          mSelectParam[i].Value = PARAM_EMPTY;
        }
      }
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
      mLanguage = language;   // memorize for localization in Execute

      for (int i = 0; i < mSelectOperation.Count; i++)
      {
        string errorMessage;
        /* dummy = */ checkAndGetScalingFactor(i, out errorMessage);
        if (errorMessage.Length > 0)
        {
          return new ValidationResult { HasError = true, Message = errorMessage };
        }
      }
      return base.Validate(language);
    }

    /// <summary>
    /// Called when the input receives a new value.
    /// </summary>
    public override void Execute()
    {
      mError.Value = "";
      try
      {
        XmlDocument xmlDoc = new XmlDocument();
        XmlReader reader = createInputReader();
        xmlDoc.Load(reader);
        for (int i = 0; i < mCount.Value;  i++)
        {
          bool success = true;  // until proven otherwise
          string outStr = "";
          double outVal = 0.0;
          if (isCombinedOutput(i))
          {
            XmlNodeList xmlNodes = xmlDoc.SelectNodes(mPath[i].Value);
            success = processNodeList(xmlNodes, i, ref outVal, ref outStr);
          }
          else
          {
            XmlNode xmlNode = xmlDoc.SelectSingleNode(mPath[i].Value);
            success = processSingleNode(xmlNode, i, ref outVal, ref outStr);
          }
          if (success)
          {
            updateOutput(i, outVal, outStr);
          }
        }
      }
      catch (Exception ex)
      {
        mError.Value += ex.Message + Environment.NewLine;
      }
    }

    XmlReader createInputReader()
    {
      XmlReader reader;
      if (mSelectInput.Value == "JSON")
      {
        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(mInput.Value);
        reader = JsonReaderWriterFactory.
            CreateJsonReader(buffer, new XmlDictionaryReaderQuotas());
      }
      else  // XML
      {
        reader = XmlReader.Create(new StringReader(mInput.Value));
      }
      return reader;
    }

    double checkAndGetScalingFactor(int i, out string errorMessage)
    {
      errorMessage = "";
      if (!isNumberOutput(i))
      {
        return 0.0;     // we need no scaling factor
      }
      string param = getSelectParam(i);
      if (param.Length <= 0)
      {
        return 1.0;     // no scaling factor given ==> 1.0
      }
      double result;
      bool success = double.TryParse(param, NumberStyles.Float,
                                     mCommaCulture, out result) ||
                     double.TryParse(param, NumberStyles.Float,
                                     mPointCulture, out result);
      if (success)
      {
        return result;  // valid scaling factor given
      }
      // None of the above ==> error
      errorMessage = Localize(mLanguage, mSelectParam[i].Name) +
                     ": \"" + mSelectParam[i].Value + "\" " +
                     Localize(mLanguage, "InvalidScaling");
      return 0.0;
    }

    bool isNumberOutput(int i)
    {
      return (mSelectOperation[i].Value == OPERATION_ADDNUMBERS) ||
             (mSelectOperation[i].Value == OPERATION_1STASNUMBER);
    }

    bool isCombinedOutput(int i)
    {
      return (mSelectOperation[i].Value == OPERATION_ADDNUMBERS) ||
             (mSelectOperation[i].Value == OPERATION_CONCATTEXTS);
    }

    string getSelectParam(int i)
    {
      if (mSelectParam[i].HasValue)
      {
        string checkParam = mSelectParam[i].Value.Trim();
        if ((checkParam.Length > 0) && (checkParam != PARAM_EMPTY))
        {
          return mSelectParam[i].Value;
        }
      }
      return "";  // not used
    }

    bool processNodeList(XmlNodeList xmlNodes, int i,
                         ref double outVal, ref string outStr)
    {
      bool isNumber = isNumberOutput(i);
      bool success = true;  //until proven otherwise
      for (int j = 0; j < xmlNodes.Count; j++)
      {
        if (isNumber)
        {
          double localVal;
          success = tryXmlConvertToDouble(i, xmlNodes[j].InnerText, out localVal);
          outVal += localVal;
        }
        else
        {
          string conStr = (j > 0) ? getSelectParam(i) : "";
          outStr = outStr + conStr + xmlNodes[j].InnerXml;
        }
      }
      return success;
    }

    bool processSingleNode(XmlNode xmlNode, int i,
                           ref double outVal, ref string outStr)
    {
      if (xmlNode != null)
      {
        bool success = true;  //until proven otherwise
        if (isNumberOutput(i))
        {
          success = tryXmlConvertToDouble(i, xmlNode.InnerText, out outVal);
        }
        else
        {
          outStr = getSelectParam(i) + xmlNode.InnerXml;
        }
        return success;
      }
      mError.Value += Localize(mLanguage, mPath[i].Name) + ": \"" +
                      mPath[i].Value + "\"" + Localize(mLanguage,
                      "NotFound") + Environment.NewLine;
      return false; // unsuccessful
    }

    bool tryXmlConvertToDouble(int i, string text, out double outVal)
    {
      try
      {
        outVal = XmlConvert.ToDouble(text);
        return true;  // successful
      }
      catch
      {
        mError.Value += Localize(mLanguage, OPERATION_PREFIX) +
                        (i+1).ToString() + ": \"" + text + "\" " +
                        Localize(mLanguage, "NoXmlDouble") + Environment.NewLine;
        outVal = 0.0;
        return false; // unsuccessful
      }
    }

    void updateOutput(int i, double outVal, string outStr)
    {
      if (isNumberOutput(i))
      {
        string errorMessage;
        double scalingFactor = checkAndGetScalingFactor(i, out errorMessage);
        if (errorMessage.Length > 0)
        { // already localized
          mError.Value += errorMessage + Environment.NewLine;
        }
        else
        {
          mOutput[i].Value = outVal * scalingFactor;
        }
      }
      else
      {
        mOutput[i].Value = outStr;
      }
    }

    /// <summary>
    /// This method is called by the framework to localize strings. We override
    /// the base class implementation to allow context dependent labeling and
    /// localization of our input and parameters.
    /// </summary>
    public override string Localize(string language, string key)
    {
      mLanguage = language;   // memorize for localization in Execute

      if (key.StartsWith(SEL_OP_PARAM_PREFIX))
      {
        string suffix = key.Substring(SEL_OP_PARAM_PREFIX.Length);
        int i = -1;
        if ( int.TryParse(suffix, out i) )
        {
          string label = mSelectOperation[i-1].Value + PARAM_POSTFIX;
          return base.Localize(language, label) + " " + suffix;
        }
      }
      if (key.StartsWith(OPERATION_PREFIX))
      {
        string suffix = key.Substring(OPERATION_PREFIX.Length);
        return base.Localize(language, OPERATION_PREFIX) + " " + suffix;
      }
      if (key.StartsWith(PATH_PREFIX))
      {
        string suffix = key.Substring(PATH_PREFIX.Length);
        return base.Localize(language, PATH_PREFIX) + " " + suffix;
      }
      return base.Localize(language, key); // OUTPUT_PREFIX and everything else
    }

  }

}
