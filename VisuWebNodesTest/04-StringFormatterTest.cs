﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LogicModule.ObjectModel;
using LogicModule.ObjectModel.TypeSystem;
using LogicModule.Nodes.TestHelper;
using NUnit.Framework;

namespace Recomedia_de.Logic.VisuWeb.Test
{
  [TestFixture]
  public class StringFormatterTest : PlaceholderTestBase<StringFormatter>
  {
    public StringFormatterTest() : base() { }

    [SetUp]
    public void StringFormatterTestSetUp()
    {
      node = new StringFormatter(context);
    }

    private readonly string mAllTypesGoodTemplate =
      "bool {:B}; int {:I}; number {:N}; percent {:P}; string {:S} end.";

    [Test]
    public void TemplParsing()
    {
      // Set a simple valid template that uses one of each kind of placeholders
      node.mTemplates[0].Value = mAllTypesGoodTemplate;
      checkDeLocalizedList<StringValueObject>(new List<string> { "Formatvorlage 1" },
                                             node.mTemplates, node.Localize);

      // Check the resulting inputs
      checkInputCounts(1, 1, 2, 1);
      checkInputNoValues();
      Assert.AreEqual("Anzahl der Ausgänge und Formatvorlagen",
                      node.Localize("de", node.mTemplateCount.Name));
      checkInputNames<BoolValueObject>(new List<string> { "BinaryInput 1" },
                                             node.mBinInputs);
      checkInputNames<IntValueObject>(new List<string> { "IntegerInput 1" },
                                             node.mIntInputs);
      checkInputNames<DoubleValueObject>(new List<string> { "NumberInput 1", "NumberInput 2" },
                                             node.mNumInputs);
      checkInputNames<StringValueObject>(new List<string> { "StringInput 1" },
                                             node.mStrInputs);
      checkDeLocalizedList<BoolValueObject>(new List<string> { "Binär 1" },
                                             node.mBinInputs, node.Localize);
      checkDeLocalizedList<IntValueObject>(new List<string> { "Ganzzahl 1" },
                                             node.mIntInputs, node.Localize);
      checkDeLocalizedList<DoubleValueObject>(new List<string> { "Zahl 1", "Zahl 2" },
                                             node.mNumInputs, node.Localize);
      checkDeLocalizedList<StringValueObject>(new List<string> { "Text 1" },
                                             node.mStrInputs, node.Localize);
      // Check the resulting output
      Assert.IsNotNull(node.mOutputs[0]);         // output should exist ...
      Assert.IsFalse(node.mOutputs[0].HasValue);  // ... bbut have no output value
      checkDeLocalizedList<IValueObject>(new List<string> { "Ausgang 1" },
                                             node.mOutputs, node.Localize);

      // Append an incomplete placeholder to the template
      node.mTemplates[0].Value = mAllTypesGoodTemplate + " {";
      // Inputs should be unchanged
      checkInputCounts(1, 1, 2, 1);
      // Set an input to update the output
      node.mBinInputs[0].Value = true;
      // Check the output
      Assert.IsTrue(node.mOutputs[0].HasValue);
      Assert.AreEqual("bool 1; int ?; number ?; percent ?; string ? end. {",
                      node.mOutputs[0].Value);
    }

    public class StringFormatterTemplateErrorTestCaseData
    {
      public static IEnumerable TemplateErrorTestCases
      {
        get
        {
          yield return new TestCaseData("", "EmptyTemplateText", true,
              "Formatvorlage 1 ist leer. Sie sollte eine gültige Kombination aus Text und Platzhaltern enthalten.",
              "Format Template 1 is empty. It should contain a valid combination of text and placeholders.")
              .SetName("TemplErr-EmptyTemplate");
          yield return new TestCaseData("abc", "NoPlaceholder", false, "", "")
              .SetName("TemplErr-NoSeparator");
          yield return new TestCaseData("{", "NoPlaceholder", false, "", "")
              .SetName("TemplErr-OneSeparatorNoText");
          yield return new TestCaseData("x{y", "NoPlaceholder", false, "", "")
              .SetName("TemplErr-OneSeparatorWithText");
          yield return new TestCaseData("{{", "NoPlaceholder", false, "", "")
              .SetName("TemplErr-UnpairedSeparatorsNoText");
          yield return new TestCaseData("x{{y", "NoPlaceholder", false, "", "")
              .SetName("TemplErr-UnpairedSeparatorWithText");
          yield return new TestCaseData("a{b{n", "NoPlaceholder", false, "", "")
              .SetName("TemplErr-UnpairedSeparatorsWithTextAndParam");
          yield return new TestCaseData("{}", "EmptyPlaceholder", false, "", "")
              .SetName("OnePlaceholderEmptyNoText");
          yield return new TestCaseData("A{}B", "EmptyPlaceholder", false, "", "")
              .SetName("OnePlaceholderEmptyWithText");
          yield return new TestCaseData("{}{}", "EmptyPlaceholder", false, "", "")
              .SetName("TwoPlaceholdersEmptyNoText");
          yield return new TestCaseData("x{}y{}z", "EmptyPlaceholder", false, "", "")
              .SetName("TwoPlaceholdersEmptyWithText");
          yield return new TestCaseData("{}{", "EmptyPlaceholder", false, "", "")
              .SetName("ThreeSeparatorsNoText1");
          yield return new TestCaseData("{{}", "EmptyPlaceholder", false, "", "")
              .SetName("ThreeSeparatorsNoText2");
          yield return new TestCaseData("{{}}", "EmptyPlaceholder", false, "", "")
              .SetName("FourSeparatorsNoText");
          yield return new TestCaseData("{a{}n{z", "EmptyPlaceholder", false, "", "")
              .SetName("FourSeparatorsUnbalanced");
          yield return new TestCaseData(String.Concat(Enumerable.Repeat("{}", 151)),
                                            "TooManyPlaceholders", false, "", "")
              .SetName("TemplErr-151SeparatorsNoText");
          yield return new TestCaseData(String.Concat(Enumerable.Repeat("{:B}", 51)),
                                            "TooManyBinPlaceholders", false, "", "")
              .SetName("TemplErr-51BinPlaceholdersNoText");
          yield return new TestCaseData(String.Concat(Enumerable.Repeat("{:I}", 51)),
                                            "TooManyIntPlaceholders", false, "", "")
              .SetName("TemplErr-51IntPlaceholdersNoText");
          yield return new TestCaseData(String.Concat(Enumerable.Repeat("{:N}", 51)),
                                            "TooManyNumPlaceholders", false, "", "")
              .SetName("TemplErr-51NumPlaceholdersNoText");
          yield return new TestCaseData(String.Concat(Enumerable.Repeat("{:P}", 51)),
                                            "TooManyNumPlaceholders", false, "", "")
              .SetName("TemplErr-51PercentPlaceholdersNoText");
          yield return new TestCaseData(String.Concat(Enumerable.Repeat("{:S}", 51)),
                                            "TooManyStrPlaceholders", false, "", "")
              .SetName("TemplErr-51StrPlaceholdersNoText");
          yield return new TestCaseData("{:U}", "PlaceholderTypeInvalid", false, "", "")
              .SetName("TemplErr-UnsupportedPlaceholderNoText");
          yield return new TestCaseData("{N}", "PlaceholderNameNotFound", false, "", "")
              .SetName("TemplErr-PlaceholderRefNotFound");
          yield return new TestCaseData("X{:U}X", "PlaceholderTypeInvalid", false, "", "")
              .SetName("TemplErr-UnsupportedPlaceholderWithText");
          yield return new TestCaseData("{:B:}", "PlaceholderMultipleColon", false, "", "")
              .SetName("TemplErr-PlaceholderMultipleColon");
          yield return new TestCaseData("{:B0}", "PlaceholderBinLengthInvalid", false, "", "")
              .SetName("TemplErr-UnsupportedBinaryPlaceholderNoText1");
          yield return new TestCaseData("X{:B101}X", "PlaceholderBinLengthInvalid", false, "", "")
              .SetName("TemplErr-UnsupportedBinaryPlaceholderWithText1");
          yield return new TestCaseData("{:Bxx}", "PlaceholderBinSameText", false, "", "")
              .SetName("TemplErr-PlaceholderBinSameText1");
          yield return new TestCaseData("{:B||}", "PlaceholderBinSameText", false, "", "")
              .SetName("TemplErr-PlaceholderBinSameText2");
          yield return new TestCaseData("{:B|xyz|xyz}", "PlaceholderBinSameText", false, "", "")
              .SetName("TemplErr-PlaceholderBinSameText3");
          yield return new TestCaseData("{:B|}", "PlaceholderBinLengthInvalid", false, "", "")
              .SetName("TemplErr-PlaceholderBinLengthInvalid");
          yield return new TestCaseData("{:B|0}", "PlaceholderBinLengthInvalid", false, "", "")
              .SetName("TemplErr-UnsupportedBinaryPlaceholderNoText2");
          yield return new TestCaseData("X{:B|1|0|1}X",
                                            "PlaceholderBinLengthInvalid", false, "", "")
              .SetName("TemplErr-UnsupportedBinaryPlaceholderWithText2");
          yield return new TestCaseData("{:B|0=false|1=true}",
                                            "PlaceholderBinInvalidAssign", false, "", "")
              .SetName("TemplErr-PlaceholderBinInvalidAssignment");
          yield return new TestCaseData("{:I1}", "PlaceholderIntLengthInvalid", false, "", "")
              .SetName("TemplErr-UnsupportedIntPlaceholderNoText");
          yield return new TestCaseData("{:I|}", "MappingEmptyImplicitValue", false, "", "")
              .SetName("TemplErr-MappingEmptyImplicitValue1");
          yield return new TestCaseData("{:I|0|}", "MappingEmptyImplicitValue", false, "", "")
              .SetName("TemplErr-MappingEmptyImplicitValue2");
          yield return new TestCaseData("{:N10}", "PlaceholderNumFormatInvalid", false, "", "")
              .SetName("TemplErr-PlaceholderNumFormatInvalidNoText");
          yield return new TestCaseData("X{:FX}X", "PlaceholderNumFormatInvalid", false, "", "")
              .SetName("TemplErr-PlaceholderNumFormatInvalidWithText");
          yield return new TestCaseData("{:N|}", "MappingNoImplicitValues", false, "", "")
              .SetName("TemplErr-MappingNoImplicitValues1");
          yield return new TestCaseData("{:F|0}", "MappingNoImplicitValues", false, "", "")
              .SetName("TemplErr-MappingNoImplicitValues2");
          yield return new TestCaseData("{:N|0=x|}", "MappingNoImplicitValues", false, "", "")
              .SetName("TemplErr-MappingNoImplicitValues3");
          yield return new TestCaseData("{:F|0|}", "MappingNoImplicitValues", false, "", "")
              .SetName("TemplErr-MappingNoImplicitValues4");
          yield return new TestCaseData("{:S0}", "PlaceholderStrLengthInvalid", false, "", "")
              .SetName("TemplErr-PlaceholderStrLengthInvalidNoText");
          yield return new TestCaseData("{T:S|alt}", "MappingNoImplicitTextValues", false, "", "")
              .SetName("TemplErr-PlaceholderMappingNoImplicitTextValues");
          yield return new TestCaseData("{T:S|=new}", "MappingNoOriginalTextValue", false, "", "")
              .SetName("TemplErr-PlaceholderMappingNoOriginalTextValue");
          yield return new TestCaseData("{T:S|x=y=z}", "MappingWrongTextAssignment", false, "", "")
              .SetName("TemplErr-PlaceholderStrInvalidMapping");
          yield return new TestCaseData("X{:Sy}X", "PlaceholderStrLengthInvalid", false, "", "")
              .SetName("TemplErr-PlaceholderStrLengthInvalidWithText");
          yield return new TestCaseData("{1Input:S}", "PlaceholderNameInvalid", false, "", "")
              .SetName("TemplErr-PlaceholderInvalidName1");
          yield return new TestCaseData("{Input 1:S}{Input 1:I}",
                                            "PlaceholderReuseWrongType", false, "", "")
              .SetName("TemplErr-PlaceholderReuseWrongType");
          yield return new TestCaseData("{Input 1:I|x=u}",
                                            "ExplicitMappingInvalidValue", false, "", "")
              .SetName("TemplErr-ExplicitMappingInvalidValue");
          yield return new TestCaseData("{Input 1:F|0..x=u}",
                                            "ExplicitMappingInvRngVal", false, "", "")
              .SetName("TemplErr-ExplicitMappingInvalidRangeValue1");
          yield return new TestCaseData("{Input 1:F|>..1=u}",
                                            "ExplicitMappingInvRngVal", false, "", "")
              .SetName("TemplErr-ExplicitMappingInvalidRangeValue2");
          yield return new TestCaseData("{Input 1:F|-1..>=u}",
                                            "ExplicitMappingInvRngVal", false, "", "")
              .SetName("TemplErr-ExplicitMappingInvalidRangeValue3");
          yield return new TestCaseData("{Input 1:F|0.1=u}",
                                            "ExplicitMappingInvalidRange", false, "", "")
              .SetName("TemplErr-ExplicitMappingInvalidRange1");
          yield return new TestCaseData("{Input 1:F|0...1=u}",
                                            "ExplicitMappingInvalidRange", false, "", "")
              .SetName("TemplErr-ExplicitMappingInvalidRange2");
          yield return new TestCaseData("{Input 1:F|1..1=u}",
                                            "ExplicitMappingInvertedRange", false, "", "")
              .SetName("TemplErr-ExplicitMappingInvertedRange1");
          yield return new TestCaseData("{Input 1:I|2..1=u}",
                                            "ExplicitMappingInvertedRange", false, "", "")
              .SetName("TemplErr-ExplicitMappingInvertedRange2");
          yield return new TestCaseData("{Input 1:N|u}", "MappingNoImplicitValues", false, "", "")
              .SetName("TemplErr-MappingNoImplicitValues");
          yield return new TestCaseData("{Input 1:I|u|2=v}", "MappingNoExplicitValues", false, "", "")
              .SetName("TemplErr-MappingNoExplicitValues");
          yield return new TestCaseData("{Input 1:I|0=1=u}", "MappingWrongAssignment", false, "", "")
              .SetName("TemplErr-MappingWrongAssignment2");
        }
      }
    }
    [TestCaseSource(typeof(StringFormatterTemplateErrorTestCaseData),
                    "TemplateErrorTestCases", Category = "TemplateErrorTestCases")]
    public void TemplateErrors(string template,
                               string expectedErrorCode,
                                 bool doesErrorRemoveInputs,
                               string expectedErrorTextDe,
                               string expectedErrorTextEn)
    {
      // Set the same simple valid template using one of each kind of placeholders
      // that is used and checked in the GoodTemplate() test case
      node.mTemplates[0].Value = mAllTypesGoodTemplate;
      // Set another template requiring another input. This input must stay intact
      // even when an error removes the inputs of the first template
      node.mTemplateCount.Value = 2;
      node.mTemplates[1].Value = "bool2 {bool2:B}";

      // Check the resulting inputs
      checkInputCounts(2, 1, 2, 1);
      // Check the output state
      Assert.IsNotNull(node.mOutputs[0]);         // output should exist ...
      Assert.IsFalse(node.mOutputs[0].HasValue);  // ... but has no output value
      Assert.IsNotNull(node.mOutputs[1]);         // output should exist ...
      Assert.IsFalse(node.mOutputs[1].HasValue);  // ... but has no output value

      // Execute the error test case with both de and en localization
      node.mTemplates[0].Value = template;
      ValidateAndCheckError("de", expectedErrorCode, expectedErrorTextDe);
      ValidateAndCheckError("en", expectedErrorCode, expectedErrorTextEn);

      // Recheck the resulting inputs
      if (doesErrorRemoveInputs)
      { // should be gone, except bool2
        checkInputCounts(1, 0, 0, 0);
      }
      else
      { // should still be there, unchanged
        checkInputCounts(2, 1, 2, 1);
      }
      // Recheck the output state; must still have no value
      Assert.IsNotNull(node.mOutputs[0]);
      Assert.IsFalse(node.mOutputs[0].HasValue);
      Assert.IsNotNull(node.mOutputs[1]);
      Assert.IsFalse(node.mOutputs[1].HasValue);
    }

    private void ValidateAndCheckError(string locale,
                                       string expectedErrorCode,
                                       string expectedErrorText)
    {
      ValidationResult result = node.Validate(locale);
      Assert.IsTrue(result.HasError);         // expect error

      // Check localized error message
      Assert.AreNotEqual(result.Message, expectedErrorCode);
      if (expectedErrorText.Length > 0)
      { // Checkk detailed error message if given
        Assert.AreEqual(expectedErrorText, result.Message);
      }
      else
      { // Otherwise expect simple localization, potentialliy preceded by
        // some prefix
        var messageDe = node.Localize(locale, expectedErrorCode);
        Assert.IsTrue(result.Message.EndsWith(messageDe));
        // Some error messages are prefixed by the offending token or an
        // input field name. Those error messages start with a blank. If
        // this is still the case in the final message, the prefix is
        // missing.
        Assert.IsFalse(result.Message.StartsWith(" "));
      }
    }

    public class StringFormatterSeparatorErrorTestCaseData
    {
      public static IEnumerable SeparatorErrorTestCases
      {
        get
        {
          yield return new TestCaseData(",", ",",
                                        "Gruppen- und Dezimaltrennzeichen dürfen nicht gleich sein.",
                                        "Group and decimal separator cannot be the same.")
                       .SetName("CustSepE-BothComma");
          yield return new TestCaseData("", "",
                                        "Das Dezimaltrennzeichen darf nicht leer sein.",
                                        "The decimal separator cannot be empty.")
                       .SetName("CustSepE-BothEmpty");
          yield return new TestCaseData(",", "",
                                        "Das Dezimaltrennzeichen darf nicht leer sein.",
                                        "The decimal separator cannot be empty.")
                       .SetName("CustSepE-DecimalSepEmpty");
          yield return new TestCaseData(",,", ",",
                                        "Das Gruppentrennzeichen darf maximal ein Zeichen haben.",
                                        "The group separator cannot be longer than one character.")
                       .SetName("CustSepE-GroupSepTooLong");
          yield return new TestCaseData(",", ",,",
                                        "Das Dezimaltrennzeichen darf maximal ein Zeichen haben.",
                                        "The decimal separator cannot be longer than one character.")
                       .SetName("CustSepE-DecimalSepTooLong");
          yield return new TestCaseData("ab", "xy",
                                        "Das Gruppentrennzeichen darf maximal ein Zeichen haben.",
                                        "The group separator cannot be longer than one character.")
                       .SetName("CustSepE-BothTooLong");
        }
      }
    }
    [TestCaseSource(typeof(StringFormatterSeparatorErrorTestCaseData),
                    "SeparatorErrorTestCases", Category = "SeparatorErrorTestCases")]
    public void CustomSeparatorErrors(string groupSeparator,
                                      string decimalSeparator,
                                      string expectedErrorDe,
                                      string expectedErrorEn)
    {
      // Customize the separators BEFORE changing templates
      node.mCustomGroupSeparator.Value = groupSeparator;
      node.mCustomDecimalSeparator.Value = decimalSeparator;

      // Set a simple valid template that uses one of each kind of placeholders
      node.mTemplates[0].Value = "{:N}";

      ValidationResult result = node.Validate("en");
      Assert.IsTrue(result.HasError);     // expect an error
      Assert.AreEqual(expectedErrorEn, result.Message);

      result = node.Validate("de");
      Assert.IsTrue(result.HasError);     // expect an error
      Assert.AreEqual(expectedErrorDe, result.Message);
    }


    public class StringFormatterSeparatorGoodTestCaseData
    {
      public static IEnumerable SeparatorGoodTestCases
      {
        get
        {
          yield return new TestCaseData("", ",", "12345678,09").SetName("GoodCustSep-German");
          yield return new TestCaseData("´", ".", "12´345´678.09").SetName("GoodCustSep-Swiss");
          // Although not distinguishable from normal (ASCII 32) space in typical source code
          // editors, the following test data uses the Unicode "Narrow no-break space" (U+202F)
          // as the group separator
          yield return new TestCaseData(" ", ",", "12 345 678,09").SetName("GoodCustSep-InternationalUnicode");
        }
      }
    }
    [TestCaseSource(typeof(StringFormatterSeparatorGoodTestCaseData),
                    "SeparatorGoodTestCases", Category = "SeparatorGoodTestCases")]
    public void GoodCustomSeparators(string groupSeparator,
                                 string decimalSeparator,
                                 string expectedOutput)
    {
      // Customize the separators BEFORE changing templates
      node.mCustomGroupSeparator.Value   = groupSeparator;
      node.mCustomDecimalSeparator.Value = decimalSeparator;

      // Set a simple valid template that uses one of each kind of placeholders
      node.mTemplates[0].Value = "{:N}";

      ValidationResult result = node.Validate("de");
      Assert.IsFalse(result.HasError);            // expect no error
      // Check the resulting inputs
      checkInputCounts(0, 0, 1, 0);
      // Check the output state
      Assert.IsNotNull(node.mOutputs[0]);         // output should exist ...
      Assert.IsFalse(node.mOutputs[0].HasValue);  // ... but have no output value

      // Assign the value and check the output
      node.mNumInputs[0].Value = 12345678.09;
      Assert.IsTrue(node.mOutputs[0].HasValue);
      Assert.AreEqual(expectedOutput, node.mOutputs[0].Value);
    }


    [Test]
    public void GoodCaseIncremental()
    {
      // Set a simple valid template that uses one of each kind of placeholders
      node.mTemplates[0].Value = mAllTypesGoodTemplate;

      // Customize the separators AFTER changing templates
      node.mCustomGroupSeparator.Value = ".";
      node.mCustomDecimalSeparator.Value = ",";

      ValidationResult result = node.Validate("de");
      Assert.IsFalse(result.HasError);        // expect no error
      // Check the resulting inputs
      checkInputCounts(1, 1, 2, 1);
      // Check the output state
      Assert.IsNotNull(node.mOutputs[0]);         // output should exist ...
      Assert.IsFalse(node.mOutputs[0].HasValue);  // ... but have no output value

      // Assign the values, one by one, and check the output each time
      node.mBinInputs[0].Value = false;
      Assert.IsTrue(node.mOutputs[0].HasValue);
      Assert.AreEqual("bool 0; int ?; number ?; percent ?; string ? end.",
                      node.mOutputs[0].Value);
      node.mIntInputs[0].Value = 42;
      Assert.IsTrue(node.mOutputs[0].HasValue);
      Assert.AreEqual("bool 0; int 42; number ?; percent ?; string ? end.",
                      node.mOutputs[0].Value);
      node.mNumInputs[0].Value = 17.0;
      Assert.IsTrue(node.mOutputs[0].HasValue);
      Assert.AreEqual("bool 0; int 42; number 17,00; percent ?; string ? end.",
                      node.mOutputs[0].Value);
      node.mNumInputs[1].Value = 0.73;
      Assert.IsTrue(node.mOutputs[0].HasValue);
      Assert.AreEqual("bool 0; int 42; number 17,00; percent 73,00%; string ? end.",
                      node.mOutputs[0].Value);
      node.mStrInputs[0].Value = "fox";
      Assert.IsTrue(node.mOutputs[0].HasValue);
      Assert.AreEqual("bool 0; int 42; number 17,00; percent 73,00%; string fox end.",
                      node.mOutputs[0].Value);
      // Re-set the numeric input to test the group separator
      node.mNumInputs[0].Value = 17000;
      Assert.IsTrue(node.mOutputs[0].HasValue);
      Assert.AreEqual("bool 0; int 42; number 17.000,00; percent 73,00%; string fox end.",
                      node.mOutputs[0].Value);
    }

    public class StringFormatterGoodTestCaseData
    {
      public static IEnumerable GoodTestCases
      {
        get
        {
          // Boolean
          yield return new TestCaseData("bool {:B}, {:B} end.",
                               new List<bool>   { false, true },
                               new List<int>    { },
                               new List<double> { },
                               new List<string> { }, true,
                               new List<string> { "BinaryInput 1", "BinaryInput 2" },
                               null, null, null,
                               "bool 0, 1 end.",
                               null, null, null, null, null
                               ).SetName("GoodC-BoolDefault");
          yield return new TestCaseData("bool {:b01}, {:b01} end.",
                               new List<bool>   { false, true },
                               new List<int> { },
                               new List<double> { },
                               new List<string> { }, true,
                               new List<string> { "BinaryInput 1", "BinaryInput 2" },
                               null, null, null,
                               "bool 0, 1 end.",
                               null, null, null, null, null
                               ).SetName("GoodC-Bool01");
          yield return new TestCaseData("bool {:BOI}, {:BOI} end.",
                               new List<bool> { false, true },
                               new List<int> { },
                               new List<double> { },
                               new List<string> { }, true,
                               new List<string> { "BinaryInput 1", "BinaryInput 2" },
                               null, null, null,
                               "bool O, I end.",
                               null, null, null, null, null
                               ).SetName("GoodC-BoolOI");
          yield return new TestCaseData("bool {:B☁☀}, {:B☁☀} end.",
                               new List<bool> { false, true },
                               new List<int> { },
                               new List<double> { },
                               new List<string> { }, true,
                               new List<string> { "BinaryInput 1", "BinaryInput 2" },
                               null, null, null,
                               "bool ☁, ☀ end.",
                               null, null, null, null, null
                               ).SetName("GoodC-BoolUnicode1");
          yield return new TestCaseData("bool {:B|🔹|🔴}, {:B|🔹|🔴} end.",
                               new List<bool> { false, true },
                               new List<int> { },
                               new List<double> { },
                               new List<string> { }, true,
                               new List<string> { "BinaryInput 1", "BinaryInput 2" },
                               null, null, null,
                               "bool 🔹, 🔴 end.",
                               null, null, null, null, null
                               ).SetName("GoodC-BoolUnicode2");
          yield return new TestCaseData("bool {:b|Länger aus|Noch länger ein}, {:b|Extrem viel länger aus|Extrem viel länger ein} end.",
                               new List<bool> { false, true },
                               new List<int> { },
                               new List<double> { },
                               new List<string> { }, true,
                               null, null, null, null,
                               "bool Länger aus, Extrem viel länger ein end.",
                               null, null, null, null, null
                               ).SetName("GoodC-BoolAusEinLang");
          yield return new TestCaseData("bool {:b|Aus|Ein}, {BinaryInput 1:B|Öff|Ön}, {:B☁☀}, {BinaryInput 1} end.",
                               new List<bool> { true, false },  // This is the only     ^
                               new List<int> { },               // one to define &      |
                               new List<double> { },            // use BinaryInput 2 ---+
                               new List<string> { }, true,
                               new List<string> { "BinaryInput 1", "BinaryInput 2" },
                               null, null, null,
                               "bool Ein, Ön, ☁, Ein end.",
                               null, null, null, null, null
                               ).SetName("GoodC-BoolReuseNoAuto");
          yield return new TestCaseData("bool {Bool :b|Aus|Ein}, { Bool:B|Öff|Ön}, { Bool :B☁☀}, {  Bool   } end.",
                               new List<bool> { true },
                               new List<int> { },
                               new List<double> { },
                               new List<string> { }, true,
                               new List<string> { "Bool" },
                               null, null, null,
                               "bool Ein, Ön, ☀, Ein end.",
                               null, null, null, null, null
                               ).SetName("GoodC-BoolReuseNamed");
          // Integer
          yield return new TestCaseData("fourtytwo {I1:I}; minus seventythree {I1_:I}",
                               new List<bool> { },
                               new List<int> { 42, -73 },
                               new List<double> { },
                               new List<string> { }, true,
                               null,
                               new List<string> { "I1", "I1_" },
                               null, null,
                               "fourtytwo 42; minus seventythree -73",
                               null, null, null, null, null
                               ).SetName("GoodC-IntDefault");
          yield return new TestCaseData("A: {input[A]:I|-1=neg|0=null|1=pos}, B: {input[B]:I|1=pos|0=null|-1=neg}, C: {input[C]:I|0=null|-1=neg|1=pos}, D: {input[D]:I|0=null|1=pos|-1=neg}",
                               new List<bool> { },
                               new List<int> { -1, 0, 1, 2 },
                               new List<double> { },
                               new List<string> { },
                               true, null,
                               new List<string> { "input[A]", "input[B]", "input[C]", "input[D]" },
                               null, null,
                               "A: neg, B: null, C: pos, D: 2",
                               null, null, null, null, null
                               ).SetName("GoodC-IntDiscrete");
          yield return new TestCaseData("A: {Input(0):I|..<0=neg|0=null|>0..=pos}, B: {Input(1):I|>0..=pos|0=null|..<0=neg}, C: {Input(2):I|0=null|..<0=neg|>0..=pos}, D: {Input(3):I|0=null|>0..=pos|..<0=neg}",
                               new List<bool> { },
                               new List<int> { -10, 0, 1, 2 },
                               new List<double> { },
                               new List<string> { },
                               true, null,
                               new List<string> { "Input(0)", "Input(1)", "Input(2)", "Input(3)" },
                               null, null,
                               "A: neg, B: null, C: pos, D: pos",
                               null, null, null, null, null
                               ).SetName("GoodC-IntRange");
          yield return new TestCaseData("A: {Int:I|..<0=neg|>0..=pos}, B: {Int:I|-10=minusTen}, C: {Int:I}, D: {Int}",
                               new List<bool> { },
                               new List<int> { -10 },
                               new List<double> { },
                               new List<string> { },
                               true, null,
                               new List<string> { "Int" },
                               null, null,
                               "A: neg, B: minusTen, C: -10, D: neg",
                               new List<bool> { },
                               new List<int> { 1 },
                               new List<double> { },
                               new List<string> { },
                               "A: pos, B: 1, C: 1, D: pos"
                               ).SetName("GoodC-IntRangeReuseIncremental");
          yield return new TestCaseData("Status {Status Bad:I|0|Komfort|Standby|Nacht|Frostschutz}, {Status Bad}, {Status Bad:I}, {Status Bad:I|1=Comfort|2=Standby|3=Night|4=Protect}",
                               new List<bool> { },
                               new List<int> { 1 },
                               new List<double> { },
                               new List<string> { },
                               true, null,
                               new List<string> { "Status Bad" },
                               null, null,
                               "Status Komfort, Komfort, 1, Comfort",
                               new List<bool> { },
                               new List<int> { 3 },
                               new List<double> { },
                               new List<string> { },
                               "Status Nacht, Nacht, 3, Night"
                               ).SetName("GoodC-IntDiscreteReuseIncremental");
          yield return new TestCaseData("Int { Int: I | 0 = null | .. 0 = negativ | .. = positiv }",
                               new List<bool> { },
                               new List<int> { 0 },
                               new List<double> { },
                               new List<string> { },
                               true, null,
                               new List<string> { "Int" },
                               null, null,
                               "Int  null ",     // second blank is from value representation
                               new List<bool> { },
                               new List<int> { int.MaxValue },
                               new List<double> { },
                               new List<string> { },
                               "Int  positiv "   // second blank is from range representation
                               ).SetName("GoodC-IntDiscreteRangesIncremental1");
          yield return new TestCaseData("Int { Int: I | 0 = null | .. 0 = negativ | .. = positiv }",
                               new List<bool> { },
                               new List<int> { 0 },
                               new List<double> { },
                               new List<string> { },
                               true, null,
                               new List<string> { "Int" },
                               null, null,
                               "Int  null ", // second blank is from value representation
                               new List<bool> { },
                               new List<int> { int.MinValue },
                               new List<double> { },
                               new List<string> { },
                               "Int  negativ "   // second blank is from range representation
                               ).SetName("GoodC-IntDiscreteRangesIncremental2");
          yield return new TestCaseData("Int { Int: I | 0 = null | .. 0 = negativ | .. = positiv }",
                               new List<bool> { },
                               new List<int> { -1 },
                               new List<double> { },
                               new List<string> { },
                               true, null,
                               new List<string> { "Int" },
                               null, null,
                               "Int  negativ ",  // second blank is from range representation
                               new List<bool> { },
                               new List<int> { 0 },
                               new List<double> { },
                               new List<string> { },
                               "Int  null "      // second blank is from value representation
                               ).SetName("GoodC-IntDiscreteRangesIncremental3");
          yield return new TestCaseData("Int { Int: I | 0 = null | .. 0 = negativ | .. = positiv }",
                               new List<bool> { },
                               new List<int> { 1 },
                               new List<double> { },
                               new List<string> { },
                               true, null,
                               new List<string> { "Int" },
                               null, null,
                               "Int  positiv ",  // second blank is from range representation
                               new List<bool> { },
                               new List<int> { 0 },
                               new List<double> { },
                               new List<string> { },
                               "Int  null "      // second blank is from value representation
                               ).SetName("GoodC-IntDiscreteRangesIncremental4");
          // Number
          yield return new TestCaseData("pi {:N}; -e {:N}",
                               new List<bool>   { },
                               new List<int> { },
                               new List<double> { 3.14159265359, -2.71828182846 },
                               new List<string> { }, true,
                               null, null,
                               new List<string> { "NumberInput 1", "NumberInput 2" },
                               null,
                               "pi 3.14; -e -2.72",
                               null, null, null, null, null
                               ).SetName("GoodC-NumDefault");
          yield return new TestCaseData("-pi {:N0}; e {:N0}",
                               new List<bool>   { },
                               new List<int> { },
                               new List<double> { -3.14159265359, 2.71828182846 },
                               new List<string> { }, true,
                               null, null, null, null,
                               "-pi -3; e 3",
                               null, null, null, null, null
                               ).SetName("GoodC-Num0");
          yield return new TestCaseData("pi {:N9}; -e {:N9}",
                               new List<bool> { },
                               new List<int> { },
                               new List<double> { 3.14159265359, -2.71828182846 },
                               new List<string> { }, true,
                               null, null, null, null,
                               "pi 3.141592654; -e -2.718281828",
                               null, null, null, null, null
                               ).SetName("GoodC-Num9");
          yield return new TestCaseData("10Mio {:F1}",
                               new List<bool> { },
                               new List<int> { },
                               new List<double> { 10e6 },
                               new List<string> { }, true,
                               null, null, null, null,
                               "10Mio 10000000.0",
                               null, null, null, null, null
                               ).SetName("GoodC-Num10MioNoSep");
          yield return new TestCaseData("10Mio {:N1}",
                               new List<bool> { },
                               new List<int> { },
                               new List<double> { 10e6 },
                               new List<string> { }, true,
                               null, null, null, null,
                               "10Mio 10'000'000.0",
                               null, null, null, null, null
                               ).SetName("GoodC-Num10MioSep");
          yield return new TestCaseData("10Mio {:G1}",
                               new List<bool> { },
                               new List<int> { },
                               new List<double> { 10e6 },
                               new List<string> { }, true,
                               null, null, null, null,
                               "10Mio 1E+07",
                               null, null, null, null, null
                               ).SetName("GoodC-Num10MioSci");
          yield return new TestCaseData("10Mio+ {:G}",
                               new List<bool> { },
                               new List<int> { },
                               new List<double> { 12345678.9 },
                               new List<string> { }, true,
                               null, null, null, null,
                               "10Mio+ 12345678.9",
                               null, null, null, null, null
                               ).SetName("GoodC-Num10MioSciDef");
          yield return new TestCaseData("10Mio+ {:G4}",
                               new List<bool> { },
                               new List<int> { },
                               new List<double> { 12345678.9 },
                               new List<string> { }, true,
                               null, null, null, null,
                               "10Mio+ 1.235E+07",
                               null, null, null, null, null
                               ).SetName("GoodC-Num10MioSci3");
          yield return new TestCaseData("A: {A:F|0=null|..0=neg|0,0..100,0=pos}, B: {B:N|>0,0..=pos|-100..<0=neg}, C: {C:f1|0=null|..0=neg|0,0..=pos}, D: {D:n1|0=null|0,0..=pos|-100..0=neg}, E: {E:n7|>0,0..20,0=pos|..<0=neg}",
                               new List<bool> { },
                               new List<int> { },
                               new List<double> { -42.42, -5.8, 0.0, 12.34, 24.68 },
                               new List<string> { },
                               true, null, null,
                               new List<string> { "A", "B", "C", "D", "E" },
                               null,
                               "A: neg, B: neg, C: null, D: pos, E: 24.6800000",
                               null, null, null, null, null
                               ).SetName("GoodC-NumRange");
          yield return new TestCaseData("F: {Number:F}; f0: {Number:f0}; n: {Number:n}; N3: {Number:N3}; G0: {Number:G0}; g3: {Number:g3}; G7: {Number:G7}; OutOfRanges: {Number:G|0..1e8=Low|2e8..=High}; InRange: {Number:G|1e8..2e8=In}; MasterFormat: {Number}",
                               new List<bool> { },
                               new List<int> { },
                               new List<double> { 123456789.123456789 },
                               new List<string> { },
                               true, null, null,
                               new List<string> { "Number" },
                               null,
                               "F: 123456789.12; f0: 123456789; n: 123'456'789.12; N3: 123'456'789.123; G0: 123456789.123457; g3: 1.23E+08; G7: 1.234568E+08; OutOfRanges: 123456789.123457; InRange: In; MasterFormat: 123456789.12",
                               null, null, null, null, null
                               ).SetName("GoodC-NumReuse");
          // Percent
          yield return new TestCaseData("{:P}",
                               new List<bool>   { },
                               new List<int> { },
                               new List<double> { 0.4273 },
                               new List<string> { }, true,
                               null, null,
                               new List<string> { "NumberInput 1" },
                               null,
                               "42.73%",
                               null, null, null, null, null
                               ).SetName("GoodC-PercentDefault");
          yield return new TestCaseData("{:p0}",
                               new List<bool>   { },
                               new List<int> { },
                               new List<double> { -0.4273 },
                               new List<string> { }, true,
                               null, null, null, null,
                               "-43%",
                               null, null, null, null, null
                               ).SetName("GoodC-Percent0");
          yield return new TestCaseData("{:P1}",
                               new List<bool> { },
                               new List<int> { },
                               new List<double> { 0.4273 },
                               new List<string> { }, true,
                               null, null, null, null,
                               "42.7%",
                               null, null, null, null, null
                               ).SetName("GoodC-Percent1");
          yield return new TestCaseData("P1: {P1:P1|..<0=⇩|>1..=⇧}, P2: {P2:P2|..<0=⇩|>1..=⇧}, P3: {P3:P3|..<0=⇩|>1..=⇧}, P4: {P4:P4|..<0=⇩|>1..=⇧}, P5: {P5:P5|..<0=⇩|>1..=⇧}",
                               new List<bool> { },
                               new List<int> { },
                               new List<double> { -0.0000001, 0, 0.4273, 1, 1.0000001 },
                               new List<string> { },
                               true, null, null,
                               new List<string> { "P1", "P2", "P3", "P4", "P5" },
                               null,
                               "P1: ⇩, P2: 0.00%, P3: 42.730%, P4: 100.0000%, P5: ⇧",
                               null, null, null, null, null
                               ).SetName("GoodC-PercentRange");
          yield return new TestCaseData("{Percent:P|..<0=⇩|0..1=|>1..=⇧}{Percent:P1}{Percent}",
                               new List<bool> { },
                               new List<int> { },
                               new List<double> { -0.001 },
                               new List<string> { },
                               true, null, null,
                               new List<string> { "Percent" },
                               null,
                               "⇩-0.1%⇩",
                               new List<bool> { },
                               new List<int> { },
                               new List<double> { 0.001 },
                               new List<string> { },
                               "0.1%"
                               ).SetName("GoodC-PercentRangeReuse");
          // String
          yield return new TestCaseData("The {:s} brown {:S} jumps {:s} the {:S} dog.",
                               new List<bool> { },
                               new List<int> { },
                               new List<double> { },
                               new List<string> { "quick", "fox", "over", "lazy" }, true,
                               null, null, null,
                               new List<string> { "StringInput 1", "StringInput 2", "StringInput 3", "StringInput 4" },
                               "The quick brown fox jumps over the lazy dog.",
                               null, null, null, null, null
                               ).SetName("GoodC-String");
          yield return new TestCaseData("{Text:S|x=⇩|y=|z=⇧}",
                               new List<bool> { },
                               new List<int> { },
                               new List<double> { },
                               new List<string> { "xyz" }, true,
                               null, null, null,
                               new List<string> { "Text" },
                               "⇩⇧",
                               null, null, null, null, null
                               ).SetName("GoodC-StringReplaceEmpty");
          yield return new TestCaseData("{Text:S|x y=bla|a z=fasel}",
                               new List<bool> { },
                               new List<int> { },
                               new List<double> { },
                               new List<string> { "x y z" }, true,
                               null, null, null,
                               new List<string> { "Text" },
                               "blfasel",
                               null, null, null, null, null
                               ).SetName("GoodC-StringReplaceOverlapping");
          // Mixed incremental
          yield return new TestCaseData("{:bOI} (seit {:s}) | {:b.X} | Tvs {:n0}/{:n0}°C | Trs {:n0}°C",
                               new List<bool>   { true, true },
                               new List<int> { },
                               new List<double> { 43.49, -43.51, 36 },
                               new List<string> { "15:23" }, true,
                               null, null, null, null,
                               "I (seit 15:23) | X | Tvs 43/-44°C | Trs 36°C",
                               new List<bool>   { false },
                               new List<int> { },
                               new List<double> { 43.5 },
                               new List<string> { "15:57" },
                               "O (seit 15:57) | X | Tvs 44/-44°C | Trs 36°C"
                               ).SetName("GoodC-MixedWithModification");
          yield return new TestCaseData("{:bOI} (seit {:s}) | {:b.X} | Tvs {:n0}/{:n0}°C | Trs {:n0}°C",
                               new List<bool>   {  },
                               new List<int> { },
                               new List<double> { 43.49 },
                               new List<string> {  }, false,
                               null, null, null, null,
                               "? (seit ?) | ? | Tvs 43/?°C | Trs ?°C",
                               new List<bool>   { true },
                               new List<int> { },
                               new List<double> { },
                               new List<string> { "16:13" },
                               "I (seit 16:13) | ? | Tvs 43/?°C | Trs ?°C"
                               ).SetName("GoodC-MixedIncremental");
        }
      }
    }
    [TestCaseSource(typeof(StringFormatterGoodTestCaseData),
                    "GoodTestCases", Category = "GoodTestCases")]
    public void GoodCaseTests(string template,
                          List<bool> binInputs,
                           List<int> intInputs,
                        List<double> numInputs,
                        List<string> strInputs,
                                bool checkInputs,
                        List<string> binInputNames,
                        List<string> intInputNames,
                        List<string> numInputNames,
                        List<string> strInputNames,
                              string expOutput,
                          List<bool> binInputMods,
                           List<int> intInputMods,
                        List<double> numInputMods,
                        List<string> strInputMods,
                              string expModOutput
)
    {
      // Set the given template
      node.mTemplates[0].Value = template;

      // Check no errors occurred during formatting
      foreach (var token in node.mTokensPerTemplate[0])
      {
        Assert.IsEmpty(token.getError());
      }

      // Check the resulting inputs
      if ( checkInputs )
      {
        bool checkBinInputNames = (null != binInputNames);
        int binCount = checkBinInputNames ? binInputNames.Count : binInputs.Count;
        bool checkIntInputNames = (null != intInputNames);
        int intCount = checkIntInputNames ? intInputNames.Count : intInputs.Count;
        bool checkNumInputNames = (null != numInputNames);
        int numCount = checkNumInputNames ? numInputNames.Count : numInputs.Count;
        bool checkStrInputNames = (null != strInputNames);
        int strCount = checkStrInputNames ? strInputNames.Count : strInputs.Count;

        checkInputCounts(binCount, intCount, numCount, strCount);

        if (checkBinInputNames)
        {
          checkInputNames<BoolValueObject>(binInputNames, node.mBinInputs);
        }
        if (checkIntInputNames)
        {
          checkInputNames<IntValueObject>(intInputNames, node.mIntInputs);
        }
        if (checkNumInputNames)
        {
          checkInputNames<DoubleValueObject>(numInputNames, node.mNumInputs);
        }
        if (checkStrInputNames)
        {
          checkInputNames<StringValueObject>(strInputNames, node.mStrInputs);
        }
      }

      // Assign the given values
      for (int i1 = 0; i1 < binInputs.Count; i1++)
      {
        node.mBinInputs[i1].Value = binInputs[i1];
      }
      for (int i2 = 0; i2 < intInputs.Count; i2++)
      {
        node.mIntInputs[i2].Value = intInputs[i2];
      }
      for (int i3 = 0; i3 < numInputs.Count; i3++)
      {
        node.mNumInputs[i3].Value = numInputs[i3];
      }
      for (int i4 = 0; i4 < strInputs.Count; i4++)
      {
        node.mStrInputs[i4].Value = strInputs[i4];
      }

      // Check the expected output
      Assert.IsTrue(node.mOutputs[0].HasValue);
      Assert.AreEqual(expOutput, node.mOutputs[0].Value);

      if ( expModOutput != null )
      {
        // Assign the given value modifications
        for (int i1 = 0; i1 < binInputMods.Count; i1++)
        {
          node.mBinInputs[i1].Value = binInputMods[i1];
        }
        for (int i2 = 0; i2 < intInputMods.Count; i2++)
        {
          node.mIntInputs[i2].Value = intInputMods[i2];
        }
        for (int i3 = 0; i3 < numInputMods.Count; i3++)
        {
          node.mNumInputs[i3].Value = numInputMods[i3];
        }
        for (int i4 = 0; i4 < strInputMods.Count; i4++)
        {
          node.mStrInputs[i4].Value = strInputMods[i4];
        }

        // Check the expected output modification
        Assert.IsTrue(node.mOutputs[0].HasValue);
        Assert.AreEqual(expModOutput, node.mOutputs[0].Value);
      }
    }

  }
}
