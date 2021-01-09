using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Text.RegularExpressions;
using LogicModule.ObjectModel;
using LogicModule.ObjectModel.TypeSystem;

namespace Recomedia_de.Logic.VisuWeb
{
  public sealed class TemplateTokenFactory
  {
    // Explicit static constructor to tell C# compiler
    // not to mark type as beforefieldinit
    static TemplateTokenFactory()
    {
    }

    private TemplateTokenFactory()
    {
      mCommaCulture = new CultureInfo("de");

      mPlaceholders = new Dictionary<string, TokenBase>();
    }

    public static TemplateTokenFactory Instance
    {
      get
      {
        return mInstance;
      }
    }

    public void restart()
    {
      defaultNameIndex =
      new int[(int)(TokenType.TokenTypeDim)] { 0, 0, 0, 0, 0, 0, 0 };
      mPlaceholders.Clear();
    }

    public TokenBase createConstStringToken(string text)
    {
      return new ConstStringToken(text);
    }

    public TokenBase createPlaceholderToken(string placeholderText,
                    string groupSeparator, string decimalSeparator)
    {
      PlaceholderInfo info = parsePlaceholder(placeholderText);
      if (info.result.HasError)
      {
        return new ErrorToken(placeholderText, info.result.Message);
      }
      else
      {
        VarTokenBase ret;

        switch (info.type)
        {
          case TokenType.VarBoolean:
            ret = new VarBooleanToken(placeholderText, info.name, info.isDefaultName, info.numericMappings);
            break;
          case TokenType.VarInteger:
            ret = new VarNumericToken<int>(placeholderText, info.type, info.name, info.isDefaultName,
                                           info.format, groupSeparator, decimalSeparator, info.numericMappings);
            break;
          case TokenType.VarNumber:
            ret = new VarNumericToken<double>(placeholderText, info.type, info.name, info.isDefaultName,
                                              info.format, groupSeparator, decimalSeparator, info.numericMappings);
            break;
          case TokenType.VarString:
            ret = new VarStringToken(placeholderText, info.name, info.isDefaultName, info.textMappings);
            break;
          case TokenType.VarReference:
            if ( info.hasType )
            {
              return new VarReferenceToken(placeholderText, info.reference, info.format, info.numericMappings);
            }
            else
            {
              return new VarReferenceToken(placeholderText, info.reference);
            }
          default:
            return new ErrorToken(placeholderText, "UnsupportedPlaceholderType");
        }
        mPlaceholders.Add(info.name, ret);
        return ret;
      }
    }

    private PlaceholderInfo parsePlaceholder(string placeholderText)
    {
      PlaceholderInfo retInfo = new PlaceholderInfo { type = TokenType.Error };
      retInfo.name = "";  // start empty

      if ( 0 < placeholderText.Length )
      {
        // Extract & process the name if one is given
        string[] s1 = placeholderText.Split(':');
        retInfo.name = s1[0].Trim();
        if ( 0 < retInfo.name.Length )
        {
          if ( !VALID_NAME_PATTERN.IsMatch(retInfo.name) )
          {
            retInfo.result.HasError = true;
            retInfo.result.Message = "PlaceholderNameInvalid";
            return retInfo;
          }
          TokenBase preToken;
          bool preFound = mPlaceholders.TryGetValue(retInfo.name, out preToken);
          if ( preFound && (preToken is VarTokenBase preVarToken) )
          { // The name already exists
            retInfo.type = TokenType.VarReference;
            retInfo.reference = preVarToken;
          }
        }

        // Extract & process the format and mappings if given after ':'
        switch ( s1.Length )
        {
          case 1:   // Name only; no format given
            if ( retInfo.type != TokenType.VarReference )
            { // name alone is reference and should have been found above
              retInfo.result.HasError = true;
              retInfo.result.Message = "PlaceholderNameNotFound";
            }
            return retInfo;
          case 2:   // Name (potentially empty) and format given
            placeholderText = s1[1];  // Format to parse follows after ':' 
            if ( 0 < placeholderText.Length )
            { // Parse the format text to determine type, number format, and mappings
              parseFormatAndMappings(placeholderText, ref retInfo);
              if (retInfo.result.HasError)
              {
                return retInfo;
              }
              retInfo.hasType = true;
            }
            break;
          default:  // More than one colon is an error
            retInfo.result.HasError = true;
            retInfo.result.Message = "PlaceholderMultipleColon";
            return retInfo;
        }

        if (0 == retInfo.name.Length)
        { // No name given, make one up
          retInfo.name = getNextDefaultName(retInfo.type);
          retInfo.isDefaultName = true;
        }
      }
      else
      {
        retInfo.result.HasError = true;
        retInfo.result.Message = "EmptyPlaceholder";
      }

      return retInfo;
    }


    private string getNextDefaultName(TokenType type)
    {
      defaultNameIndex[(int)type]++;
      return PREFIX[(int)type] + " " + defaultNameIndex[(int)type].ToString();
    }

    private void parseFormatAndMappings(string formatText,
                                ref PlaceholderInfo outInfo)
    {
      string[] formatTokens = formatText.Split('|');
      formatTokens[0] = formatTokens[0].ToUpper().Trim();
      TokenType type = getType(formatTokens[0]);
      switch ( type )
      {
        case TokenType.VarBoolean:
          parseBooleanPlaceholder(formatTokens, ref outInfo);
          break;
        case TokenType.VarInteger:
          parseIntegerPlaceholder(formatTokens, ref outInfo);
          break;
        case TokenType.VarNumber:
          parseNumberPlaceholder(formatTokens, ref outInfo);
          break;
        case TokenType.VarString:
          parseStringPlaceholder(formatTokens, ref outInfo);
          break;
        default:
          outInfo.result.HasError = true;
          outInfo.result.Message = "PlaceholderTypeInvalid";
          break;
      }

      if ( outInfo.type == TokenType.VarReference )
      {
        if ( outInfo.reference.getType() != type )
        {
          outInfo.result.HasError = true;
          outInfo.result.Message = "PlaceholderReuseWrongType";
        }
      }
      else
      {
        outInfo.type = type;
      }
    }

    private TokenType getType(string typeToken)
    {
      if (typeToken.Length > 0)
      {
        switch (typeToken[0])
        {
          case 'B':
            return TokenType.VarBoolean;
          case 'I':
            return TokenType.VarInteger;
          case 'F':
          case 'G':
          case 'N':
          case 'P':
            return TokenType.VarNumber;
          case 'S':
            return TokenType.VarString;
          default:
            return TokenType.Error;
        }
      }
      return TokenType.Error;
    }

    private void parseBooleanPlaceholder(string[] formatTokens,
                   ref PlaceholderInfo outInfo)
    {
      var m0 = new NumericMapping { minValue = 0, maxValue = 0 };
      var m1 = new NumericMapping { minValue = 1, maxValue = 1 };

      switch ( formatTokens.Length )
      {
        case 1:
          switch (formatTokens[0].Length)
          {
            case 1:
              // nothing to do
              break;
            case 3:
              m0.representation = formatTokens[0][1].ToString();
              m1.representation = formatTokens[0][2].ToString();
              break;
            default:
              outInfo.result.HasError = true;
              outInfo.result.Message = "PlaceholderBinLengthInvalid";
              return;
          }
          break;
        case 3:
          m0.representation = formatTokens[1];
          m1.representation = formatTokens[2];
          break;
        default:
          outInfo.result.HasError = true;
          outInfo.result.Message = "PlaceholderBinLengthInvalid";
          return;
      }
      if ( (null != m0.representation) && (null != m1.representation) )
      {
        outInfo.numericMappings = new List<NumericMapping>();
        outInfo.numericMappings.Add(m0);
        outInfo.numericMappings.Add(m1);

        if (!isBinTextValid(m0.representation) || !isBinTextValid(m1.representation))
        {
          outInfo.result.HasError = true;
          outInfo.result.Message = "PlaceholderBinInvalidAssign";
        }

        if (m0.representation == m1.representation)
        {
          outInfo.result.HasError = true;
          outInfo.result.Message = "PlaceholderBinSameText";
        }
      }
    }

    private bool isBinTextValid(string s)
    {
      if ( s.Contains('=') )
      {
        return false;
      }
      return true;
    }

    private void parseIntegerPlaceholder(string[] formatTokens,
                   ref PlaceholderInfo outInfo)
    {
      System.Diagnostics.Trace.Assert(formatTokens.Length > 0);
      if (formatTokens[0].Length != 1)
      {
        outInfo.result.HasError = true;
        outInfo.result.Message = "PlaceholderIntLengthInvalid";
        return;
      }
      outInfo.format = "G";   // shortest possible number format for integers
      parseNumericMappings(/* allowImplicitValues = */ true, formatTokens, ref outInfo);
    }

    private void parseNumberPlaceholder(string[] formatTokens,
                  ref PlaceholderInfo outInfo)
    {
      System.Diagnostics.Trace.Assert(formatTokens.Length > 0);
      if ( (formatTokens[0].Length == 1) ||
            ((formatTokens[0].Length == 2) && (char.IsDigit(formatTokens[0][1]))) )
      { // No or one digit parameter, use as number format as-is
        outInfo.format = formatTokens[0];
      }
      else
      {
        outInfo.result.HasError = true;
        outInfo.result.Message = "PlaceholderNumFormatInvalid";
        return;
      }
      parseNumericMappings(/* allowImplicitValues = */ false, formatTokens, ref outInfo);
    }

    private void parseNumericMappings(bool allowImplicitValues,
                           string[] formatTokens,
                ref PlaceholderInfo outInfo)
    {
      outInfo.numericMappings = null;
      if (formatTokens.Length > 1)
      {
        bool allowExplicitValues = true;  // until an implicit value has been found
        int implicitValue = 0;
        // Ignore entry at index 0 (type/precision processed by caller)
        outInfo.numericMappings = new List<NumericMapping>(formatTokens.Length - 1);
        for (int i = 1; i < formatTokens.Length; i++)
        {
          string[] valueRepresentation = formatTokens[i].Split('=');
          switch (valueRepresentation.Length)
          {
            case 1:
              if (allowImplicitValues)
              {
                NumericMapping mapping = new NumericMapping
                {
                  minValue = implicitValue,
                  maxValue = implicitValue,
                  representation = valueRepresentation[0]
                };
                if (0 == mapping.representation.Length)
                {
                  outInfo.result.HasError = true;
                  outInfo.result.Message = "MappingEmptyImplicitValue";
                  return;
                }

                outInfo.numericMappings.Add(mapping);
                allowExplicitValues = false;
                implicitValue++;
              }
              else
              {
                outInfo.result.HasError = true;
                outInfo.result.Message = "MappingNoImplicitValues";
                return;
              }
              break;
            case 2:
              if (allowExplicitValues)
              {
                parseExplicitNumericMapping(valueRepresentation, ref outInfo);
                if (outInfo.result.HasError)
                {
                  return;
                }
                allowImplicitValues = false;
              }
              else
              {
                outInfo.result.HasError = true;
                outInfo.result.Message = "MappingNoExplicitValues";
                return;
              }
              break;
            default:
              outInfo.result.HasError = true;
              outInfo.result.Message = "MappingWrongAssignment";
              return;
          }
        }
      }
    }

    private void parseExplicitNumericMapping(string[] valueRepresentation,
                           ref PlaceholderInfo outInfo)
    {
      System.Diagnostics.Trace.Assert(valueRepresentation.Length == 2);

      string[] rangeStrings = valueRepresentation[0].Split('.');
      if ( rangeStrings.Length == 1 )
      {
        DoubleVal value = parseValue(rangeStrings[0]);
        if ( value.isValid )
        {
          NumericMapping mapping = new NumericMapping  {
            minValue = value.value,
            maxValue = value.value,
            representation = valueRepresentation[1]
          };
          outInfo.numericMappings.Add(mapping);
        }
        else
        {
          outInfo.result.HasError = true;
          outInfo.result.Message = "ExplicitMappingInvalidValue";
          return;
        }
      }
      else if ( (rangeStrings.Length == 3) && (rangeStrings[1].Length == 0) )
      {
        DoubleVal minValue = parseRangeValue(rangeStrings[0], /* isMin = */ true);
        DoubleVal maxValue = parseRangeValue(rangeStrings[2], /* isMin = */ false);
        if ( minValue.isValid && maxValue.isValid )
        {
          if ( minValue.value < maxValue.value )
          {
            NumericMapping mapping = new NumericMapping {
              minValue = minValue.value,
              isMinValueExcluded = minValue.isExcluded,
              maxValue = maxValue.value,
              isMaxValueExcluded = maxValue.isExcluded,
              representation = valueRepresentation[1]
            };
            outInfo.numericMappings.Add(mapping);
          }
          else
          {
            outInfo.result.HasError = true;
            outInfo.result.Message = "ExplicitMappingInvertedRange";
            return;
          }
        }
        else
        {
          outInfo.result.HasError = true;
          outInfo.result.Message = "ExplicitMappingInvRngVal";
          return;
        }
      }
      else
      {
        outInfo.result.HasError = true;
        outInfo.result.Message = "ExplicitMappingInvalidRange";
        return;
      }
    }

    private DoubleVal parseRangeValue(string s0, bool isMin)
    {
      DoubleVal val = new DoubleVal { // empty translates to numeric range boundary
        value   = isMin ? Double.MinValue : Double.MaxValue,
        isValid = true
      };
      string s1 = s0.Trim();
      if ( s1.Length > 0 )
      {
        bool isExcluded = false;
        string s2;

        if ( (isMin && (s1[0] == '>')) || (!isMin && (s1[0] == '<')) )
        {
          isExcluded = true;
          s2 = s1.Substring(1);
        }
        else
        {
          s2 = s1;
        }
        val = parseValue(s2);
        val.isExcluded = isExcluded;
      }
      return val;
    }

    private DoubleVal parseValue(string s0)
    {
      string s1 = s0.Trim();
      DoubleVal val = new DoubleVal { value = 0.0, isValid = false };
      val.isValid = Double.TryParse(s1, NumberStyles.Float,
                                    mCommaCulture, out val.value);
      return val;
    }

    private void parseStringPlaceholder(string[] formatTokens,
                  ref PlaceholderInfo outInfo)
    {
      if ( ( formatTokens.Length > 0 ) &&
           ( (formatTokens[0].Length < 0) || (formatTokens[0].Length > 1) ) )
      {
        outInfo.result.HasError = true;
        outInfo.result.Message = "PlaceholderStrLengthInvalid";
        return;
      }
      parseStringMappings(formatTokens, ref outInfo);
    }

    private void parseStringMappings(string[] formatTokens,
                          ref PlaceholderInfo outInfo)
    {
      // Ignore entry at index 0 (type processed by caller)
      outInfo.textMappings = null;
      if (formatTokens.Length > 1)
      {
        outInfo.textMappings = new List<TextMapping>(formatTokens.Length - 1);
        for (int i = 1; i < formatTokens.Length; i++)
        {
          string[] valueRepresentation = formatTokens[i].Split('=');
          switch (valueRepresentation.Length)
          {
            case 1:
              outInfo.result.HasError = true;
              outInfo.result.Message = "MappingNoImplicitTextValues";
              return;
            case 2:
              parseExplicitTextMapping(valueRepresentation, ref outInfo);
              if (outInfo.result.HasError)
              {
                return;
              }
              break;
            default:
              outInfo.result.HasError = true;
              outInfo.result.Message = "MappingWrongTextAssignment";
              return;
          }
        }
      }
    }

    private void parseExplicitTextMapping(string[] valueRepresentation,
                               ref PlaceholderInfo outInfo)
    {
      System.Diagnostics.Trace.Assert(valueRepresentation.Length == 2);

      if (valueRepresentation[0].Length > 0)
      {
        TextMapping mapping = new TextMapping
        {
          original = valueRepresentation[0],
          representation = valueRepresentation[1]
        };
        outInfo.textMappings.Add(mapping);
      }
      else
      {
        outInfo.result.HasError = true;
        outInfo.result.Message = "MappingNoOriginalTextValue";
      }
    }

    private struct PlaceholderInfo
    {
      public ValidationResult     result;
      public string               name;
      public TokenType            type;
      public string               format;
      public List<NumericMapping> numericMappings;
      public List<TextMapping>    textMappings;
      public VarTokenBase         reference;
      public bool                 hasType;
      public bool                 isDefaultName;
    }

    private struct DoubleVal
    {
      public double value;
      public bool   isValid;
      public bool   isExcluded;
    }

    private readonly CultureInfo mCommaCulture;

    // Valid names start with a letter and optionally continue with letters,
    // digits, blanks or punctuation (except :, {, and }, because these create
    // ambiguity with surrounding syntax elements).
    private readonly Regex VALID_NAME_PATTERN =
      new Regex(@"^[\p{L}][\p{L}\p{N}\p{P}\p{Zs}]*$");

    private readonly string[] PREFIX =
      new string[(int)(TokenType.TokenTypeDim)] { "ERROR",
                                                  "BinaryInput",
                                                  "IntegerInput",
                                                  "NumberInput",
                                                  "StringInput",
                                                  "REFERENCE",
                                                  "CONST_STRING" };
    private int[] defaultNameIndex =
      new int[(int)(TokenType.TokenTypeDim)] { 0, 0, 0, 0, 0, 0, 0 };

    private Dictionary<string, TokenBase> mPlaceholders;

    private static TemplateTokenFactory mInstance = new TemplateTokenFactory();
  }

}
