using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LogicModule.ObjectModel;
using LogicModule.Nodes.TestHelper;
using NUnit.Framework;


namespace Recomedia_de.Logic.Generic.Test
{
  [TestFixture]
  public class StatisticsTest
  {
    
    public StatisticsTest()
    {
      context = TestNodeContext.Create();
    }

    private readonly INodeContext context;
    private Statistics node;

    [SetUp]
    public void StatisticsTestSetUp()
    {
      node = new Statistics(context);
    }

    [TearDown]
    public void StatisticsTestTearDown()
    {
      node = null;
    }

    [Test]
    public void OutputPropertiesTest()
    {
      // Expected output object type names
      var expectedValueTypeName = "DoubleValueObject";
      var expectedIndexTypeName = "IntValueObject";

      // Check initial state
      Assert.IsNotNull(node.mOutput1);   // should be double
      Assert.AreEqual(expectedValueTypeName, node.mOutput1.GetType().Name);
      Assert.AreEqual("Avg", node.mOutput1.Name);
      Assert.IsNull(node.mOutput2);      // should be off
      Assert.IsNull(node.mOutputIndex);  // should be off

      // Check for all possible settings of node.SelectedFunction
      foreach (var priFunc in node.mSelectedFunction.AllowedValues)
      {
        node.mSelectedFunction.Value = priFunc;
        node.updateOutputValues();

        switch (priFunc)
        {
          case "Min":
          case "Max":
          case "Sum":
          case "Avg":
          case "StdDev":
            Assert.IsNotNull(node.mOutput1);   // should be double
            Assert.AreEqual(expectedValueTypeName, node.mOutput1.GetType().Name);
            Assert.AreEqual(priFunc, node.mOutput1.Name);
            Assert.IsFalse(node.mOutput1.HasValue);
            Assert.IsNull(node.mOutput2);      // should be off
            Assert.IsNull(node.mOutputIndex);  // should be off
            break;
          default:
            Assert.Fail("Unhandled primary function");
            break;
        }
      }

      // Check for all possible settings of node.SecondaryFunction
      foreach ( var secFunc in node.mSecondaryFunction.AllowedValues )
      {
        node.mSecondaryFunction.Value = secFunc;
        node.updateOutputValues();

        switch ( secFunc )
        {
          case "none":
            Assert.IsNotNull(node.mOutput1);
            Assert.AreEqual(expectedValueTypeName, node.mOutput1.GetType().Name);
            Assert.AreEqual("StdDev", node.mOutput1.Name);
            Assert.IsNull(node.mOutput2);
            Assert.IsNull(node.mOutputIndex);
            break;
          case "MinIndex":
          case "MaxIndex":
            Assert.IsNotNull(node.mOutput1);
            Assert.AreEqual(expectedValueTypeName, node.mOutput1.GetType().Name);
            Assert.AreEqual("StdDev", node.mOutput1.Name);
            Assert.IsNull(node.mOutput2);
            Assert.IsNotNull(node.mOutputIndex);
            Assert.AreEqual(expectedIndexTypeName, node.mOutputIndex.GetType().Name);
            Assert.AreEqual(secFunc, node.mOutputIndex.Name);
            break;
          case "Min":
          case "Max":
          case "Sum":
          case "Avg":
          case "StdDev":
            Assert.IsNotNull(node.mOutput1);
            Assert.AreEqual(expectedValueTypeName, node.mOutput1.GetType().Name);
            Assert.AreEqual("StdDev", node.mOutput1.Name);
            Assert.IsNotNull(node.mOutput2);
            Assert.AreEqual(expectedValueTypeName, node.mOutput2.GetType().Name);
            Assert.AreEqual(secFunc, node.mOutput2.Name);
            Assert.IsNull(node.mOutputIndex);
            break;
          default:
            Assert.Fail("Unhandled secondary function");
            break;
        }
      }
    }

    [Test]
    public void ValidateTest()
    {
      // Default values must validate without error
      var result = node.Validate("en");
      Assert.IsFalse(result.HasError);

      // Setting the secondary function to the primary function default must fail
      node.mSecondaryFunction.Value = "Avg";
      result = node.Validate("en");
      Assert.IsTrue(result.HasError);
      Assert.AreEqual("The function for output 2 cannot be the same as for output 1.",
                      result.Message);
      result = node.Validate("de");
      Assert.IsTrue(result.HasError);
      Assert.AreEqual("Die Funktion für Ausgang 2 kann nicht dieselbe sein wie für Ausgang 1.",
                      result.Message);

      // Changing the primary function to some other value must pass
      node.mSecondaryFunction.Value = "Max";
      result = node.Validate("en");
      Assert.IsFalse(result.HasError);
    }

    public class StatisticsTestCaseData
    {
      public static IEnumerable MinTestCases
      {
        get
        {
          yield return new TestCaseData(new List<double> { 0, 0, 0 },
                                 "Min", new List<double> { 0, 0, 0 },
                            "MinIndex", new List<double> { 0, 0, 0 }).SetName("Output-Min: All values set to 0");
          yield return new TestCaseData(new List<double> { 49, -211, -132, 85 },
                                 "Min", new List<double> { 49, -211, -211, -211 },
                            "MaxIndex", new List<double> { 0, 0, 0, 3 }).SetName("Output-Min: Mixed values");
          yield return new TestCaseData(new List<double> { 491, -2511, -1132, -985, double.MinValue },
                                 "Min", new List<double> { 491, -2511, -2511, -2511, double.MinValue },
                                 "Max", new List<double> { 491, 491, 491, 491, 491 }).SetName("Output-Min: MinValue");
          yield return new TestCaseData(new List<double> { 491, -2511, -1132, -985, double.MaxValue },
                                 "Min", new List<double> { 491, -2511, -2511, -2511, -2511 },
                            "MinIndex", new List<double> { 0, 1, 1, 1, 1 }).SetName("Output-Min: MaxValue");
        }
      }
      public static IEnumerable MaxTestCases
      {
        get
        {
          yield return new TestCaseData(new List<double> { 0, 0, 0 },
                                 "Max", new List<double> { 0, 0, 0 },
                            "MaxIndex", new List<double> { 0, 0, 0 }).SetName("Output-Max: All values set to 0");
          yield return new TestCaseData(new List<double> { -1, -21, 0, 32, 18, -42, 0, 39 },
                                 "Max", new List<double> { -1, -1, 0, 32, 32, 32, 32, 39 },
                                 "Sum", new List<double> { -1, -22, -22, 10, 28, -14, -14, 25 }).
                                 SetName("Output-Max: Mixed values positive, negative and zero");
          yield return new TestCaseData(new List<double> { double.MaxValue, 5, 3 },
                                 "Max", new List<double> { double.MaxValue, double.MaxValue, double.MaxValue },
                                "none", null).SetName("Output-Max: MaxValue");
          yield return new TestCaseData(new List<double> { double.MinValue, 4, 182 },
                                 "Max", new List<double> { double.MinValue, 4, 182 },
                            "MaxIndex", new List<double> { 0, 1, 2 }).SetName("Output-Max: MinValue");
        }
      }
      public static IEnumerable SumTestCases
      {
        get
        {
          yield return new TestCaseData(new List<double> { 0, 0, 0 },
                                 "Sum", new List<double> { 0, 0, 0 },
                                 "Avg", new List<double> { 0, 0, 0 }).
                                 SetName("Output-Sum: All values set to 0");
          yield return new TestCaseData(new List<double> { 1, 21, 32, 15 },
                                 "Sum", new List<double> { 1, 22, 54, 69 },
                                 "Avg", new List<double> { 1, 11, 54.0/3, 69.0/4 }).
                                 SetName("Output-Sum: All values are positive");
          yield return new TestCaseData(new List<double> { 1, -54, 21, -82, 0, 32, 15 },
                                 "Sum", new List<double> { 1, -53, -32, -114, -114, -82, -67 },
                                 "Min", new List<double> { 1, -54, -54, -82, -82, -82, -82 }).
                                 SetName("Output-Sum: Mixed values");
        }
      }
      public static IEnumerable AvgTestCases
      {
        get
        {
          yield return new TestCaseData(new List<double> { 0, 0, 0 },
                                 "Avg", new List<double> { 0, 0, 0 },
                                "none", null).SetName("Output-Avg: All values set to 0");
          yield return new TestCaseData(new List<double> { 10, -10, 0 },
                                 "Avg", new List<double> { 10, 0, 0 },
                              "StdDev", new List<double> { 0, 10, Math.Sqrt(200.0 / 3) }).
                              SetName("Output-Avg: Simple test");
          yield return new TestCaseData(new List<double> { 5, 10, 75, 0, -45, 20, 30, -25.0 },
                                 "Avg", new List<double> { 5, 7.5, 30, 90.0/4, 9, 65.0/6, 95.0/7, 8.75 },
                                "none", null).SetName("Output-Avg: Mixed values");
        }
      }
      public static IEnumerable StdDevTestCases
      {
        get
        {
          yield return new TestCaseData(new List<double> { 0, 0, 0 },
                              "StdDev", new List<double> { 0, 0, 0 },
                                "none", null).SetName("Output-StdDev: All values set to 0");
          yield return new TestCaseData(new List<double> { 10, -10, 0 },
                              "StdDev", new List<double> { 0, 10, Math.Sqrt(200.0/3) },
                                 "Sum", new List<double> { 10, 0, 0 }).SetName("Output-StdDev: Simple test");
          yield return new TestCaseData(new List<double> { 5, 10, 75, 0, -45, 20, 30, -25.0,
                                                           8.75, 42.2449784333, -21.22758490406 },
                              "StdDev", new List<double> { 0, 2.5, 31.8852107828, 30.5163890393, 38.3927076409,
                                                           35.2865255996, 33.3503357998, 33.7036719068,
                                                           31.7761266082, 31.7761266082, 31.7761266082 },
                                "none", null).SetName("Output-StdDev: Mixed values");
        }
      }
    }
    [TestCaseSource(typeof(StatisticsTestCaseData), "MinTestCases", Category = "MaxOutputTests")]
    [TestCaseSource(typeof(StatisticsTestCaseData), "MaxTestCases", Category = "MinOutputTests")]
    [TestCaseSource(typeof(StatisticsTestCaseData), "SumTestCases", Category = "SumOutputTests")]
    [TestCaseSource(typeof(StatisticsTestCaseData), "StdDevTestCases", Category = "StdDevOutputTests")]
    public void OutputAnyTests(IList<double> inputs, string function, IList<double> expected,
                                                    string function2, IList<double> expected2 )
    {
      node.mInputCount.Value = inputs.Count;
      node.mSelectedFunction.Value = function;
      node.mSecondaryFunction.Value = function2;
      node.updateOutputValues();

      Assert.IsNotNull(node.mOutput1);   // should be double
      Assert.IsFalse(node.mOutput1.HasValue);
      Assert.IsTrue((null == node.mOutput2) || !node.mOutput2.HasValue);
      Assert.IsTrue((null == node.mOutputIndex) || !node.mOutputIndex.HasValue);

      foreach (var item in inputs.Select((value, index) => new {value, index}))
      {
        node.mInputs[item.index].Value = item.value;
        // No need to call Execute(), because we use a ValueSet handler instead.
        // We call it only -- as the logic engine does -- after all inputs have
        // been set, and only to ensure that it doesn't do any harm.
        if (item.index >= (inputs.Count - 1))
        {
          node.Execute();
        }

        // Test primary value output
        Assert.IsTrue(node.mOutput1.HasValue);
        switch (function)
        {
          case "StdDev":    // less accurate expected values provided
            Assert.AreEqual(expected[item.index], node.mOutput1.Value, 5e-11);
            break;
          default:          // accurate expected values provided
            Assert.AreEqual(expected[item.index], node.mOutput1.Value);
            break;
        }
        // Test secondary value output
        switch ( function2 )
        {
          case "none":      // test case sanity check
            Assert.IsNull(expected2);
            Assert.IsNull(node.mOutput2);
            break;
          case "MinIndex":  // real test for indices
          case "MaxIndex":
            Assert.IsNotNull(expected2);
            Assert.IsTrue(node.mOutputIndex.HasValue);
            Assert.AreEqual(expected2[item.index], node.mOutputIndex.Value);
            break;
          default:         // real test for values
            Assert.IsNotNull(expected2);
            Assert.IsTrue(node.mOutput2.HasValue);
            Assert.AreEqual(expected2[item.index], node.mOutput2.Value);
            break;
        }
      }
    }
  }
}