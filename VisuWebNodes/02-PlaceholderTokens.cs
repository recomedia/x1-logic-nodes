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

  public struct Mapping
  {
    public double minValue;
    public double maxValue;
    public bool   isMinValueExcluded;
    public bool   isMaxValueExcluded;
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
                        List<Mapping> mappings)
      : base(source, type)
    {
      mName          = name;
      mIsDefaultName = isDefaultName;
      mMappings      = mappings;
    }

    public string getName()
    {
      return mName;
    }

    public abstract string getFormat();

    public List<Mapping> getMappings()
    {
      return mMappings;
    }

    public override bool hasDefaultName()
    {
      return mIsDefaultName;
    }

    public override bool hasFormatOrMappings()
    {
      string format = getFormat();
      bool hasFormat = ( (null != format) && (format.Length > 1) );
      bool hasMappings = ( (null != mMappings) && (mMappings.Count > 0) );
      return hasFormat || hasMappings;
    }

    public void setInput(IValueObject input)
    {
      mInput = input;
    }

    public abstract string getText(string format, List<Mapping> mappings);

    public sealed override string getText()
    {
      return getText(getFormat(), getMappings());
    }

    protected List<Mapping> mMappings;
    protected IValueObject  mInput;

    private string mName;
    private bool   mIsDefaultName;
  }

  class VarBooleanToken : VarTokenBase
  {
    public VarBooleanToken(string source,
                           string name, bool isDefaultName,
                           List<Mapping> mappings)
      : base(source,
          TokenType.VarBoolean, name, isDefaultName, mappings)
    {
    }

    public override string getFormat()
    {
      return "";  // we don't have or need a format
    }

    public override string getText(string unusedFormat, List<Mapping> mappings)
    {
      if ( mInput is BoolValueObject input )
      {
        if ( !input.HasValue )
        {
          return "?";
        }
        if ( (null != mappings) && (mappings.Count == 2) )
        {
          if (input.Value)
          {
            return mappings[1].representation;
          }
          return mappings[0].representation;
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
      if ( (null != mMappings) && (mMappings.Count != 2) )
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
                           string format, List<Mapping> mappings)
      : base(source, type, name, isDefaultName, mappings)
    {
      mFormat = format;
    }

    public override string getFormat()
    {
      return mFormat;
    }

    public override string getText(string format, List<Mapping> mappings)
    {
      if (mInput is NumericValueObject<T> input)
      {
        if (input.HasValue)
        {
          if ( null != mappings )
          {
            foreach (Mapping mapping in mappings)
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
          return input.Value.ToString(format, CultureInfo.CurrentCulture);
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

    private string mFormat;
  }

  class VarStringToken : VarTokenBase
  {
    public VarStringToken(string source,
                          string name, bool isDefaultName)
      : base(source, TokenType.VarString,
             name, isDefaultName, new List<Mapping>())
    {
    }

    public override string getFormat()
    {
      return "";  // we don't have or need a format
    }

    public override string getText(string unusedFormat, List<Mapping> unusedMappings)
    {
      if (mInput is StringValueObject input)
      {
        if (input.HasValue)
        {
          return input.Value;
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
             baseToken.getMappings())
    {
      mBaseToken = baseToken;
      mFormat = baseToken.getFormat();
    }

    public VarReferenceToken(string source, VarTokenBase baseToken,
                             string format, List<Mapping> mappings)
    : base(source, TokenType.VarReference,
           baseToken.getName(), baseToken.hasDefaultName(),
           mappings)
    {
      mBaseToken = baseToken;
      mFormat = format;
    }

    public override string getFormat()
    {
      return mFormat;
    }

    public override string getText(string format, List<Mapping> mappings)
    {
      return mBaseToken.getText(format, mappings);
    }

    public override string getError()
    {
      return mBaseToken.getError();
    }

    private VarTokenBase mBaseToken;
    private string       mFormat;
  }

}
