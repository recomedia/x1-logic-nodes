using System;
using System.IO;
using System.Net;
using System.Globalization;
using LogicModule.ObjectModel;
using LogicModule.ObjectModel.TypeSystem;
using NUnit.Framework;
using System.Diagnostics;
using System.Threading;
using LogicModule.Nodes.TestHelper;

namespace Recomedia_de.Logic.VisuWeb.Test
{
  [TestFixture]
  public class XmlJsonParserTest
  {
    protected INodeContext context;
    protected XmlJsonParser node;

    public XmlJsonParserTest()
    {
      context = TestNodeContext.Create();
    }

    [SetUp]
    public void XmlJsonParserTestSetUp()
    {
      node = new XmlJsonParser(context);
    }

    [TearDown]
    public void XmlJsonParserTearDown()
    {
      node = null;
    }

    [Test]
    public void Xml()
    {
      // Use four paths
      node.mCount.Value = 8;
      Assert.AreEqual(node.mCount.Value, node.mPath.Count);
      Assert.AreEqual(node.mCount.Value, node.mOutput.Count);
      Assert.AreEqual(node.mCount.Value, node.mSelectOperation.Count);
      Assert.AreEqual(node.mCount.Value, node.mSelectParam.Count);
      node.mPath[0].Value = "/weatherdata/location/name";
      node.mPath[1].Value = "//time[position()<=8]/precipitation/@value";
      node.mSelectOperation[1].Value = "MultiAddNumbers";
      node.mPath[2].Value = "/weatherdata//time[1]/precipitation/@value";
      node.mSelectOperation[2].Value = "FirstAsNumber";
      node.mSelectParam[2].Value = "2,5"; // standard decimal separator
      node.mPath[3].Value = "/weatherdata/forecast/time[40]/clouds/@value";
      node.mPath[4].Value = "(//temperature[@unit='celsius']/@value)[position()<=4]";
      node.mSelectOperation[4].Value = "MultiConcatTexts";
      node.mSelectParam[4].Value = " | ";
      node.mPath[5].Value = "(//humidity[@unit='%']/@value)[position()<=6]";
      node.mSelectOperation[5].Value = "MultiConcatTexts";
      node.mPath[6].Value = "(//temperature[@unit='celsius']/@value)[position()<=4]";
      node.mSelectOperation[6].Value = "MultiMinNumber";
      node.mPath[7].Value = "(//temperature[@unit='celsius']/@value)[position()<=4]";
      node.mSelectOperation[7].Value = "MultiMaxNumber";

      // Expect no validation error
      var result = node.Validate("de");
      Assert.IsFalse(result.HasError);

      // Check the initial output states
      for (int i = 0; i < node.mCount.Value; i++)
      {
        Assert.IsNotNull(node.mOutput[i]);
        Assert.AreEqual(PortTypes.Any, node.mOutput[i].PortType.Name);
        Assert.IsFalse(node.mOutput[i].HasValue);  // no output value yet
      }
      Assert.IsNotNull(node.mError);
      Assert.AreEqual(PortTypes.String, node.mError.PortType.Name);
      Assert.IsFalse(node.mError.HasValue);       // no output value yet
      Assert.AreEqual("2,5", node.mSelectParam[2].Value);

      // Set an input value and re-check the outputs
      node.mInput.Value = File.ReadAllText(@"../../openweather.xml");
      node.Execute();
      Assert.IsNotNull(node.mError);
      Assert.IsTrue(node.mError.HasValue);
      Assert.AreEqual("", node.mError.Value);     // empty error message
      Assert.IsNotNull(node.mOutput[0]);
      Assert.IsTrue(node.mOutput[0].HasValue);   // now has an output value
      Assert.AreEqual("Boblingen", node.mOutput[0].Value);
      Assert.IsNotNull(node.mOutput[1]);
      Assert.IsTrue(node.mOutput[1].HasValue);   // now has an output value
      Assert.AreEqual(0.625 + 0.063 + 0.125 + 0.313 +
                      0.188 + 0.500 + 0.188 + 0.062, node.mOutput[1].Value);
      Assert.IsNotNull(node.mOutput[2]);
      Assert.IsTrue(node.mOutput[2].HasValue);   // now has an output value
      Assert.AreEqual(0.625 * 2.5, node.mOutput[2].Value);
      Assert.IsNotNull(node.mOutput[3]);
      Assert.IsTrue(node.mOutput[3].HasValue);   // now has an output value
      Assert.AreEqual("broken clouds", node.mOutput[3].Value);
      Assert.IsNotNull(node.mOutput[4]);
      Assert.IsTrue(node.mOutput[4].HasValue);   // now has an output value
      Assert.AreEqual("11.21 | 10.94 | 10.78 | 11.07", node.mOutput[4].Value);
      Assert.IsNotNull(node.mOutput[5]);
      Assert.IsTrue(node.mOutput[5].HasValue);   // now has an output value
      Assert.AreEqual("969697968274", node.mOutput[5].Value);
      Assert.IsNotNull(node.mOutput[6]);
      Assert.IsTrue(node.mOutput[6].HasValue);   // now has an output value
      Assert.AreEqual(10.78, node.mOutput[6].Value);
      Assert.IsNotNull(node.mOutput[7]);
      Assert.IsTrue(node.mOutput[7].HasValue);   // now has an output value
      Assert.AreEqual(11.21, node.mOutput[7].Value);

      // Configure scaling factors, re-execute and re-check
      node.mSelectParam[2].Value = "7,5";
      node.mSelectParam[6].Value = "3,5";
      node.mSelectParam[7].Value = "1,5";
      node.Execute();
      Assert.IsNotNull(node.mError);
      Assert.IsTrue(node.mError.HasValue);
      Assert.AreEqual("", node.mError.Value);     // empty error message
      Assert.IsNotNull(node.mOutput[0]);
      Assert.IsTrue(node.mOutput[0].HasValue);   // now has an output value
      Assert.AreEqual("Boblingen", node.mOutput[0].Value);
      Assert.IsNotNull(node.mOutput[1]);
      Assert.IsTrue(node.mOutput[1].HasValue);   // now has an output value
      Assert.AreEqual(0.625 + 0.063 + 0.125 + 0.313 +
                      0.188 + 0.500 + 0.188 + 0.062, node.mOutput[1].Value);
      Assert.IsNotNull(node.mOutput[2]);
      Assert.IsTrue(node.mOutput[2].HasValue);   // now has an output value
      Assert.AreEqual(0.625 * 7.5, node.mOutput[2].Value);
      Assert.IsNotNull(node.mOutput[3]);
      Assert.IsTrue(node.mOutput[3].HasValue);   // now has an output value
      Assert.AreEqual("broken clouds", node.mOutput[3].Value);
      Assert.IsNotNull(node.mOutput[4]);
      Assert.IsTrue(node.mOutput[4].HasValue);   // now has an output value
      Assert.AreEqual("11.21 | 10.94 | 10.78 | 11.07", node.mOutput[4].Value);
      Assert.IsNotNull(node.mOutput[5]);
      Assert.IsTrue(node.mOutput[5].HasValue);   // now has an output value
      Assert.AreEqual("969697968274", node.mOutput[5].Value);
      Assert.IsNotNull(node.mOutput[6]);
      Assert.IsTrue(node.mOutput[6].HasValue);   // now has an output value
      Assert.AreEqual(10.78 * 3.5, node.mOutput[6].Value);
      Assert.IsNotNull(node.mOutput[7]);
      Assert.IsTrue(node.mOutput[7].HasValue);   // now has an output value
      Assert.AreEqual(11.21 * 1.5, node.mOutput[7].Value);

      // Reduce number of terms and re-check
      node.mCount.Value = 3;
      Assert.AreEqual(3, node.mPath.Count);
      Assert.AreEqual(3, node.mOutput.Count);
      Assert.AreEqual(3, node.mSelectOperation.Count);
      Assert.AreEqual(3, node.mSelectParam.Count);
      Assert.AreEqual("/weatherdata/location/name", node.mPath[0].Value);
      Assert.AreEqual("//time[position()<=8]/precipitation/@value", node.mPath[1].Value);
      Assert.AreEqual("MultiAddNumbers", node.mSelectOperation[1].Value);
      Assert.AreEqual("/weatherdata//time[1]/precipitation/@value", node.mPath[2].Value);
      Assert.AreEqual("FirstAsNumber", node.mSelectOperation[2].Value);
      Assert.AreEqual("7,5", node.mSelectParam[2].Value);

      // Change scaling factor, re-execute and re-check
      node.mSelectParam[2].Value = "7.5"; // alternate decimal separator
      node.Execute();
      Assert.IsNotNull(node.mError);
      Assert.IsTrue(node.mError.HasValue);
      Assert.AreEqual("", node.mError.Value);     // empty error message
      Assert.IsNotNull(node.mOutput[0]);
      Assert.IsTrue(node.mOutput[0].HasValue);   // now has an output value
      Assert.AreEqual("Boblingen", node.mOutput[0].Value);
      Assert.IsNotNull(node.mOutput[1]);
      Assert.IsTrue(node.mOutput[1].HasValue);   // now has an output value
      Assert.AreEqual(0.625 + 0.063 + 0.125 + 0.313 +
                      0.188 + 0.500 + 0.188 + 0.062, node.mOutput[1].Value);
      Assert.IsNotNull(node.mOutput[2]);
      Assert.IsTrue(node.mOutput[2].HasValue);   // now has an output value
      Assert.AreEqual(0.625 * 7.5, node.mOutput[2].Value);
    }

    [Test]
    public void Json()
    {
      // Use three paths
      node.mSelectInput.Value = "JSON";
      node.mCount.Value = 3;
      Assert.AreEqual(3, node.mPath.Count);
      Assert.AreEqual(3, node.mOutput.Count);
      node.mPath[0].Value = "/root/city/name";
      node.mPath[1].Value = "//list//humidity";
      node.mPath[2].Value = "/root/list/item[40]/weather/item[1]/description";
      node.mSelectParam[2].Value = "Weather text: ";  // text prefix
      // Expect no validation error
      var result = node.Validate("de");
      Assert.IsFalse(result.HasError);

      // Check the initial output states
      Assert.IsNotNull(node.mOutput[0]);
      Assert.AreEqual(PortTypes.Any, node.mOutput[0].PortType.Name);
      Assert.IsFalse(node.mOutput[0].HasValue);  // no output value yet
      Assert.IsNotNull(node.mOutput[1]);
      Assert.AreEqual(PortTypes.Any, node.mOutput[1].PortType.Name);
      Assert.IsFalse(node.mOutput[1].HasValue);  // no output value yet
      Assert.IsNotNull(node.mOutput[2]);
      Assert.AreEqual(PortTypes.Any, node.mOutput[2].PortType.Name);
      Assert.IsFalse(node.mOutput[2].HasValue);  // no output value yet
      Assert.IsNotNull(node.mError);
      Assert.AreEqual(PortTypes.String, node.mError.PortType.Name);
      Assert.IsFalse(node.mError.HasValue);       // no output value yet

      // Set an input value and re-check the outputs
      node.mInput.Value = File.ReadAllText(@"../../openweather.json");
      node.Execute();
      Assert.IsNotNull(node.mError);
      Assert.IsTrue(node.mError.HasValue);
      Assert.AreEqual("", node.mError.Value);     // empty error message
      Assert.IsNotNull(node.mOutput[0]);
      Assert.IsTrue(node.mOutput[0].HasValue);   // now has an output value
      Assert.AreEqual("Boblingen", node.mOutput[0].Value);
      Assert.IsNotNull(node.mOutput[1]);
      Assert.IsTrue(node.mOutput[1].HasValue);   // now has an output value
      Assert.AreEqual(76, int.Parse(node.mOutput[1].Value.ToString(), CultureInfo.InvariantCulture));
      Assert.IsNotNull(node.mOutput[2]);
      Assert.IsTrue(node.mOutput[2].HasValue);   // now has an output value
      Assert.AreEqual("Weather text: light rain", node.mOutput[2].Value);
    }

    [Test]
    public void XmlDocError()
    {
      // Use just one path
      Assert.AreEqual(1, node.mPath.Count);
      Assert.AreEqual(1, node.mOutput.Count);
      node.mPath[0].Value = "/some/@path";

      // Expect no validation error
      var result = node.Validate("de");
      Assert.IsFalse(result.HasError);

      // Check the initial output state
      Assert.IsNotNull(node.mOutput[0]);
      Assert.AreEqual(PortTypes.Any, node.mOutput[0].PortType.Name);
      Assert.IsFalse(node.mOutput[0].HasValue);  // no output value yet

      // Set an invalid XML input value and re-check the outputs
      node.mInput.Value = "<some path=\"value\">";
      node.Execute();
      Assert.IsNotNull(node.mError);
      Assert.IsTrue(node.mError.HasValue);
      Assert.AreEqual("Unerwartetes Dateiende. Die folgenden Elemente wurden nicht " +
                      "geschlossen: some. Zeile 1, Position 20.\r\n", node.mError.Value);
      Assert.IsNotNull(node.mOutput[0]);
      Assert.IsFalse(node.mOutput[0].HasValue);   // still has no output value
      Assert.IsNull(node.mOutput[0].Value);
    }

    [Test]
    public void JsonPathError()
    {
      // Use just one default path
      node.mSelectInput.Value = "JSON";
      Assert.AreEqual(1, node.mPath.Count);
      Assert.AreEqual(1, node.mOutput.Count);
      node.mPath[0].Value = "city/name";

      // Expect no validation error
      var result = node.Validate("de");
      Assert.IsFalse(result.HasError);

      // Check the initial output states
      Assert.IsNotNull(node.mOutput[0]);
      Assert.AreEqual(PortTypes.Any, node.mOutput[0].PortType.Name);
      Assert.IsFalse(node.mOutput[0].HasValue);  // no output value yet

      // Set an input value and re-check the outputs
      node.mInput.Value = File.ReadAllText(@"../../openweather.json");
      node.Execute();
      Assert.IsNotNull(node.mError);
      Assert.IsTrue(node.mError.HasValue);
      Assert.AreEqual("Pfad 1: \"city/name\" wurde im Eingabetext nicht gefunden.\r\n",
                      node.mError.Value);
      Assert.IsNotNull(node.mOutput[0]);
      Assert.IsFalse(node.mOutput[0].HasValue);   // still has no output value
      Assert.IsNull(node.mOutput[0].Value);
    }

    [Test]
    public void JsonPathSum()
    {
      node.mSelectInput.Value = "JSON";
      node.mCount.Value = 2;
      Assert.AreEqual(2, node.mPath.Count);
      Assert.AreEqual(2, node.mOutput.Count);
      node.mPath[0].Value = "//list//rain/three_hours";
      node.mSelectOperation[0].Value = "MultiAddNumbers";
      node.mPath[1].Value = "//list/item[position()<4]/rain/three_hours";
      node.mSelectOperation[1].Value = "MultiAddNumbers";

      // Expect no validation error
      var result = node.Validate("de");
      Assert.IsFalse(result.HasError);

      // Check the initial output states
      for (int i = 0; i < node.mCount.Value; i++)
      {
        Assert.IsNotNull(node.mOutput[i]);
        Assert.AreEqual(PortTypes.Any, node.mOutput[i].PortType.Name);
        Assert.IsFalse(node.mOutput[i].HasValue);  // no output value yet
      }
      Assert.IsNotNull(node.mError);
      Assert.AreEqual(PortTypes.String, node.mError.PortType.Name);
      Assert.IsFalse(node.mError.HasValue);       // no output value yet

      // Set an input value and re-check the outputs
      node.mInput.Value = File.ReadAllText(@"../../openweather.json");
      node.Execute();
      Assert.IsNotNull(node.mError);
      Assert.IsTrue(node.mError.HasValue);        // still no error
      Assert.AreEqual("", node.mError.Value);
      Assert.IsNotNull(node.mOutput[0]);
      Assert.IsTrue(node.mOutput[0].HasValue);    // now has output values
      Assert.AreEqual(28.625, node.mOutput[0].Value);
      Assert.IsNotNull(node.mOutput[1]);
      Assert.IsTrue(node.mOutput[1].HasValue);
      Assert.AreEqual(0.0, node.mOutput[1].Value);
    }

    [Test]
    public void JsonPathMinMax()
    {
      node.mSelectInput.Value = "JSON";
      node.mCount.Value = 4;
      Assert.AreEqual(4, node.mPath.Count);
      Assert.AreEqual(4, node.mOutput.Count);
      node.mPath[0].Value = "//list//humidity";
      node.mSelectOperation[0].Value = "MultiMinNumber";
      node.mPath[1].Value = "//list//humidity";
      node.mSelectOperation[1].Value = "MultiMaxNumber";
      node.mPath[2].Value = "//list//nonex";
      node.mSelectOperation[2].Value = "MultiMinNumber";
      node.mPath[3].Value = "//list//nonex";
      node.mSelectOperation[3].Value = "MultiMaxNumber";

      // Expect no validation error
      var result = node.Validate("de");
      Assert.IsFalse(result.HasError);

      // Check the initial output states
      for (int i = 0; i < node.mCount.Value; i++)
      {
        Assert.IsNotNull(node.mOutput[i]);
        Assert.AreEqual(PortTypes.Any, node.mOutput[i].PortType.Name);
        Assert.IsFalse(node.mOutput[i].HasValue);  // no output value yet
      }
      Assert.IsNotNull(node.mError);
      Assert.AreEqual(PortTypes.String, node.mError.PortType.Name);
      Assert.IsFalse(node.mError.HasValue);       // no output value yet

      // Set an input value and re-check the outputs
      node.mInput.Value = File.ReadAllText(@"../../openweather.json");
      node.Execute();
      Assert.IsNotNull(node.mError);
      Assert.IsTrue(node.mError.HasValue);        // still no error
      Assert.AreEqual("", node.mError.Value);
      Assert.IsNotNull(node.mOutput[0]);
      Assert.IsTrue(node.mOutput[0].HasValue);    // now has output values
      Assert.AreEqual(50.0, node.mOutput[0].Value);
      Assert.IsNotNull(node.mOutput[1]);
      Assert.IsTrue(node.mOutput[1].HasValue);
      Assert.AreEqual(95.0, node.mOutput[1].Value);
      Assert.IsNotNull(node.mOutput[2]);
      Assert.IsTrue(node.mOutput[2].HasValue);
      Assert.IsTrue(Double.IsNaN(Convert.ToDouble(node.mOutput[2].Value)));
      Assert.IsNotNull(node.mOutput[3]);
      Assert.IsTrue(node.mOutput[3].HasValue);
      Assert.IsTrue(Double.IsNaN(Convert.ToDouble(node.mOutput[3].Value)));
    }

    [Test]
    public void XmlScalingError()
    {
      // Use just one path
      Assert.AreEqual(1, node.mPath.Count);
      Assert.AreEqual(1, node.mOutput.Count);
      Assert.AreEqual(1, node.mSelectOperation.Count);
      Assert.AreEqual(1, node.mSelectParam.Count);
      node.mPath[0].Value = "/weatherdata//time[1]/precipitation/@value";
      node.mSelectOperation[0].Value = "FirstAsNumber";
      node.mSelectParam[0].Value = "2.500,7";

      // Check parameter label localization
      string label = node.mSelectParam[0].Name;
      Assert.IsTrue(label.StartsWith("SelOperParam"));
      Assert.IsTrue(node.Localize("de", label).StartsWith("Skalierungsfaktor"));
      Assert.IsTrue(node.Localize("en", label).StartsWith("Scaling factor"));

      // Expect validation error
      var result = node.Validate("de");
      Assert.IsTrue(result.HasError);
      Assert.AreEqual("Skalierungsfaktor 1: \"2.500,7\"  ist kein gültiger Skalierungsfaktor. " +
                      "Als Dezimaltrennzeichen muss Komma (,) oder Punkt (.) verwendet werden. " +
                      "Gruppentrennzeichen (Tausendertrennzeichen) sind nicht zulässig.",
                      result.Message);

      // Expect execution error, no value
      node.mInput.Value = File.ReadAllText(@"../../openweather.xml");
      node.Execute();
      Assert.IsNotNull(node.mError);
      Assert.IsTrue(node.mError.HasValue);
      Assert.AreEqual("Skalierungsfaktor 1: \"2.500,7\"  ist kein gültiger Skalierungsfaktor. " +
                      "Als Dezimaltrennzeichen muss Komma (,) oder Punkt (.) verwendet werden. " +
                      "Gruppentrennzeichen (Tausendertrennzeichen) sind nicht zulässig.\r\n",
                      node.mError.Value);
      Assert.IsNotNull(node.mOutput[0]);
      Assert.IsFalse(node.mOutput[0].HasValue);   // still has no output value
      Assert.IsNull(node.mOutput[0].Value);

      // Update operation; still expect validation error
      node.mSelectOperation[0].Value = "MultiAddNumbers";
      result = node.Validate("de");
      Assert.IsTrue(result.HasError);
      Assert.AreEqual("Skalierungsfaktor 1: \"2.500,7\"  ist kein gültiger Skalierungsfaktor. " +
                      "Als Dezimaltrennzeichen muss Komma (,) oder Punkt (.) verwendet werden. " +
                      "Gruppentrennzeichen (Tausendertrennzeichen) sind nicht zulässig.",
                      result.Message);

      // Re-execute; still expect execution error, no value
      node.Execute();
      Assert.IsNotNull(node.mError);
      Assert.IsTrue(node.mError.HasValue);
      Assert.AreEqual("Skalierungsfaktor 1: \"2.500,7\"  ist kein gültiger Skalierungsfaktor. " +
                      "Als Dezimaltrennzeichen muss Komma (,) oder Punkt (.) verwendet werden. " +
                      "Gruppentrennzeichen (Tausendertrennzeichen) sind nicht zulässig.\r\n",
                      node.mError.Value);
      Assert.IsNotNull(node.mOutput[0]);
      Assert.IsFalse(node.mOutput[0].HasValue);   // still has no output value
      Assert.IsNull(node.mOutput[0].Value);
    }

    [Test]
    public void JsonNoDoubleError()
    {
      // Use just one default path
      node.mSelectInput.Value = "JSON";
      Assert.AreEqual(1, node.mPath.Count);
      Assert.AreEqual(1, node.mOutput.Count);
      node.mPath[0].Value = "/root/list/item[1]/weather/item[1]/icon";
      node.mSelectOperation[0].Value = "FirstAsNumber";

      // Expect no validation error
      var result = node.Validate("de");
      Assert.IsFalse(result.HasError);

      // Check the initial output states
      Assert.IsNotNull(node.mOutput[0]);
      Assert.AreEqual(PortTypes.Any, node.mOutput[0].PortType.Name);
      Assert.IsFalse(node.mOutput[0].HasValue);  // no output value yet

      // Set an input value and re-check the outputs
      node.mInput.Value = File.ReadAllText(@"../../openweather.json");
      node.Execute();
      Assert.IsNotNull(node.mError);
      Assert.IsTrue(node.mError.HasValue);
      Assert.AreEqual("Art der Pfadauswahl 1: Der Text \"01n\" kann nicht in eine " +
        "Zahl umgewandelt werden. (Die Eingabezeichenfolge hat das falsche Format.)\r\n",
        node.mError.Value);
      Assert.IsNotNull(node.mOutput[0]);
      Assert.IsFalse(node.mOutput[0].HasValue);   // still has no output value
      Assert.IsNull(node.mOutput[0].Value);
    }

    [Test]
    public void JsonSpecialCharacters()
    {
      // Use just one default path
      node.mSelectInput.Value = "JSON";
      Assert.AreEqual(1, node.mPath.Count);
      Assert.AreEqual(1, node.mOutput.Count);
      node.mPath[0].Value = "/root/word";
      node.mSelectOperation[0].Value = "FirstAsText";

      // Expect no validation error
      var result = node.Validate("de");
      Assert.IsFalse(result.HasError);

      // Check the initial output states
      Assert.IsNotNull(node.mOutput[0]);
      Assert.AreEqual(PortTypes.Any, node.mOutput[0].PortType.Name);
      Assert.IsFalse(node.mOutput[0].HasValue);  // no output value yet

      // Set an input text containing a selection of special characters
      const string text = "'îáèäöü?!§$%&/()=?";
      node.mInput.Value = "{\"word\":\"" + text + "\"}";
      node.Execute();
      // Expect no error
      Assert.IsFalse(result.HasError);
      Assert.IsNotNull(node.mError);
      Assert.IsTrue(node.mError.HasValue);
      Assert.AreEqual("", node.mError.Value);     // empty error message
      // Re-check the output state
      Assert.IsNotNull(node.mOutput[0]);
      Assert.IsTrue(node.mOutput[0].HasValue);   // now has an output value
      Assert.IsNotNull(node.mOutput[0].Value);
      Assert.AreEqual(node.mOutput[0].Value, text);
    }

    [Test]
    public void XmlSpecialCharacters()
    {
      // Use just one default path
      Assert.AreEqual(1, node.mPath.Count);
      Assert.AreEqual(1, node.mOutput.Count);
      node.mPath[0].Value = "/word";
      node.mSelectOperation[0].Value = "FirstAsText";

      // Expect no validation error
      var result = node.Validate("de");
      Assert.IsFalse(result.HasError);

      // Check the initial output states
      Assert.IsNotNull(node.mOutput[0]);
      Assert.AreEqual(PortTypes.Any, node.mOutput[0].PortType.Name);
      Assert.IsFalse(node.mOutput[0].HasValue);  // no output value yet

      // Set an input text containing a selection of special characters
      const string text = "'îáèäöü?!§$%&/()=?";
      node.mInput.Value = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><word>" +
        WebUtility.HtmlEncode(text) + "</word>";
      node.Execute();
      // Expect no error
      Assert.IsFalse(result.HasError);
      Assert.IsNotNull(node.mError);
      Assert.IsTrue(node.mError.HasValue);
      Assert.AreEqual("", node.mError.Value);     // empty error message
      // Re-check the output state
      Assert.IsNotNull(node.mOutput[0]);
      Assert.IsTrue(node.mOutput[0].HasValue);   // now has an output value
      Assert.IsNotNull(node.mOutput[0].Value);
      Assert.AreEqual(node.mOutput[0].Value, text);
    }

  }

}
