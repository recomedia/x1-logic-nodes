using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using LogicModule.ObjectModel;
using LogicModule.ObjectModel.TypeSystem;

namespace Recomedia_de.Logic.VisuWeb
{
  public enum TokenType
  {
    Error = 0,
    VarBoolean = 1,
    VarInteger = 2,
    VarNumber = 3,
    VarString = 4,
    VarReference = 5,
    ConstString = 6,
    TokenTypeDim = 7
  }

  public struct NumericMapping
  {
    public double minValue;
    public double maxValue;
    public bool isMinValueExcluded;
    public bool isMaxValueExcluded;
    public string representation;
  }

  public struct TextMapping
  {
    public string original;
    public string representation;
  }

  // Interface and base implementation for all tokens of the format template
  // (constant texts and placeholders)
  public abstract class TokenBase
  {
    public TokenBase(string source, TokenType type)
    {
      mSource = source;
      mType   = type;
    }

    public string getSource()
    {
      return mSource;
    }

    public TokenType getType()
    {
      return mType;
    }

    public virtual bool hasFormatOrMappings()
    {
      return false;
    }

    public virtual bool hasDefaultName()
    {
      return false;
    }

    public virtual string getText()
    {
      return getSource();
    }

    public virtual string getError()
    {
      return "";
    }

    private string    mSource;
    private TokenType mType;
  }

  class ConstStringToken : TokenBase
  {
    public ConstStringToken(string text)
      : base(text, TokenType.ConstString)
    {
    }
  }

  class ErrorToken : TokenBase
  {
    public ErrorToken(string source, string errorCode)
      : base(source, TokenType.Error)
    {
      mErrorCode = errorCode;
    }

    public override string getError()
    {
      return mErrorCode;
    }

    private string mErrorCode;
  }

  // Base implementation for variable tokens (placeholders)
  public abstract class VarTokenBase : TokenBase
  {
    public VarTokenBase(string source, TokenType type,
                        string name, bool isDefaultName,
                        List<NumericMapping> numericMappings,
                        List<TextMapping> textMappings)
      : base(source, type)
    {
      mName            = name;
      mIsDefaultName   = isDefaultName;
      mNumericMappings = numericMappings;
      mTextMappings    = textMappings;
    }

    public string getName()
    {
      return mName;
    }

    public abstract string getFormat();

    public List<NumericMapping> getNumericMappings()
    {
      return mNumericMappings;
    }

    public List<TextMapping> getTextMappings()
    {
      return mTextMappings;
    }

    public override bool hasDefaultName()
    {
      return mIsDefaultName;
    }

    public override bool hasFormatOrMappings()
    {
      string format = getFormat();
      bool hasFormat = ((null != format) && (format.Length > 1));
      bool hasNumericMappings = ((null != mNumericMappings) && (mNumericMappings.Count > 0));
      bool hasTextMappings = ((null != mTextMappings) && (mTextMappings.Count > 0));
      return hasFormat || hasNumericMappings || hasTextMappings;
    }

    public void setInput(IValueObject input)
    {
      mInput = input;
    }

    public abstract string getText(string format,
      List<NumericMapping> numericMappings, List<TextMapping> textMappings);

    public sealed override string getText()
    {
      return getText(getFormat(), getNumericMappings(), getTextMappings());
    }

    protected List<NumericMapping> mNumericMappings;
    protected List<TextMapping> mTextMappings;
    protected IValueObject  mInput;

    private string mName;
    private bool   mIsDefaultName;
  }

  class VarBooleanToken : VarTokenBase
  {
    public VarBooleanToken(string source,
                           string name, bool isDefaultName,
                           List<NumericMapping> numericMappings)
      : base(source, TokenType.VarBoolean, name, isDefaultName, numericMappings, null)
    {
    }

    public override string getFormat()
    {
      return "";  // we don't have or need a format
    }

    public override string getText(string unusedFormat,
      List<NumericMapping> numericMappings, List<TextMapping> unusedTextMappings)
    {
      if ( mInput is BoolValueObject input )
      {
        if ( !input.HasValue )
        {
          return "?";
        }
        if ( (null != numericMappings) && (numericMappings.Count == 2) )
        {
          if (input.Value)
          {
            return numericMappings[1].representation;
          }
          return numericMappings[0].representation;
        }
        else
        {
          if (input.Value)
          {
            return "1";
          }
          return "0";
        }
      }
      return getError();
    }

    public override string getError()
    {
      if ( (null != mNumericMappings) && (mNumericMappings.Count != 2) )
        return "WrongNumberOfBooleanMappings";
      else if ( !(mInput is BoolValueObject) )
        return "InputIsNoBoolean";
      return base.getError();
    }
  }

  class VarNumericToken<T> : VarTokenBase
    where T : struct, IComparable, IFormattable
  {
    public VarNumericToken(string source, TokenType type,
                           string name, bool isDefaultName,
                           string format, string groupSeparator, string decimalSeparator,
                           List<NumericMapping> numericMappings)
      : base(source, type, name, isDefaultName, numericMappings, null)
    {
      // We use the format as-is
      mFormat = format;

      // Formatting is based on the current culture, with some customizations
      mNumberInfo = CultureInfo.CurrentCulture.NumberFormat.Clone() as NumberFormatInfo;
      // Customize group and decimal separators
      mNumberInfo.NumberGroupSeparator = groupSeparator;
      mNumberInfo.NumberDecimalSeparator = decimalSeparator;
      mNumberInfo.PercentGroupSeparator = groupSeparator;
      mNumberInfo.PercentDecimalSeparator = decimalSeparator;
      // No blank between a Number and the percent sign when using 'P' formats
      mNumberInfo.PercentPositivePattern = 1;
      mNumberInfo.PercentNegativePattern = 1;
      // We are not using the currency format, so there is no need to customize it
    }

    public override string getFormat()
    {
      return mFormat;
    }

    public override string getText(string format,
      List<NumericMapping> numericMappings, List<TextMapping> unusedTextMappings)
    {
      if (mInput is NumericValueObject<T> input)
      {
        if (input.HasValue)
        {
          if ( null != numericMappings)
          {
            foreach (NumericMapping mapping in numericMappings)
            {
              double inputValue = Convert.ToDouble(input.Value);
              bool isMinValueOk = mapping.isMinValueExcluded ?
                                      (inputValue > mapping.minValue) :
                                      (inputValue >= mapping.minValue);
              bool isMaxValueOk = mapping.isMaxValueExcluded ?
                                      (inputValue < mapping.maxValue) :
                                      (inputValue <= mapping.maxValue);
              if ( isMinValueOk && isMaxValueOk )
              {
                return mapping.representation;
              }
            }
          }
          return input.Value.ToString(format, mNumberInfo);
        }
        return "?";
      }
      return getError();
    }

    public override string getError()
    {
      if (!(mInput is NumericValueObject<T>))
        return "InputIsNotExpectedNumericType";
      return base.getError();
    }

    private string           mFormat;
    private NumberFormatInfo mNumberInfo;
  }

  class VarStringToken : VarTokenBase
  {
    public VarStringToken(string source,
                          string name, bool isDefaultName, List<TextMapping> textMappings)
      : base(source, TokenType.VarString, name, isDefaultName, null, textMappings)
    {
    }

    public override string getFormat()
    {
      return "";  // we don't have or need a format
    }

    public override string getText(string unusedFormat,
      List<NumericMapping> unusedNumericMappings, List<TextMapping> textMappings)
    {
      if (mInput is StringValueObject input)
      {
        if (input.HasValue)
        {
          string outText = input.Value;
          if (null != textMappings)
          {
            foreach (TextMapping mapping in textMappings)
            {
              outText = outText.Replace(mapping.original, mapping.representation);
            }
          }
          return outText;
        }
        return "?";
      }
      return getError();
    }
  }

  class VarReferenceToken : VarTokenBase
  {
    public VarReferenceToken(string source, VarTokenBase baseToken)
      : base(source, TokenType.VarReference,
             baseToken.getName(), baseToken.hasDefaultName(),
             baseToken.getNumericMappings(), baseToken.getTextMappings())
    {
      mBaseToken = baseToken;
      mFormat = baseToken.getFormat();
    }

    public VarReferenceToken(string source, VarTokenBase baseToken,
                             string format, List<NumericMapping> numericMappings)
    : base(source, TokenType.VarReference,
           baseToken.getName(), baseToken.hasDefaultName(),
           numericMappings, null)
    {
      mBaseToken = baseToken;
      mFormat = format;
    }

    public override string getFormat()
    {
      return mFormat;
    }

    public override string getText(string format,
      List<NumericMapping> numericMappings, List<TextMapping> textMappings)
    {
      return mBaseToken.getText(format, numericMappings, textMappings);
    }

    public override string getError()
    {
      return mBaseToken.getError();
    }

    private VarTokenBase mBaseToken;
    private string       mFormat;
  }

}
