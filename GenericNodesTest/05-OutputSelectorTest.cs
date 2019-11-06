using System;
using LogicModule.Nodes.TestHelper;
using LogicModule.ObjectModel;
using LogicModule.ObjectModel.TypeSystem;
using NUnit.Framework;

namespace Recomedia_de.Logic.Generic.Test
{
  [TestFixture]
  public class OutputSelectorTest
  {
    private INodeContext context;
    private OutputSelector node;

    public OutputSelectorTest()
    {
      context = TestNodeContext.Create();
    }

    [SetUp]
    public void OutputSelectorTestSetUp()
    {
      node = new OutputSelector(context);
    }

    [TearDown]
    public void OutputSelectorTestTearDown()
    {
      node = null;
    }

    [Test]
    public void TestNumberOfOutputs()
    {
      // Check initial state
      Assert.AreEqual(2, node.mOutputCount.Value);
      Assert.AreEqual("ResendCurrent", node.mSelectAction.Value);
      Assert.IsNull(node.mIdleValue); // no idle value by default
      Assert.IsFalse(node.mInput.HasValue); // no input value yet
      Assert.IsFalse(node.mSelectIndexInput.HasValue); // no input selected yet
      Assert.AreEqual(2, node.mOutputs.Count);
      foreach (var output in node.mOutputs)
      {
        Assert.IsFalse(output.HasValue); // no output values yet
      }

      // Increase number of outputs and re-check
      node.mOutputCount.Value = 7;
      Assert.AreEqual("ResendCurrent", node.mSelectAction.Value);
      Assert.IsNull(node.mIdleValue); // no idle value by default
      Assert.IsFalse(node.mInput.HasValue); // no input value yet
      Assert.IsFalse(node.mSelectIndexInput.HasValue); // no input selected yet
      Assert.AreEqual(7, node.mOutputs.Count);
      foreach (var output in node.mOutputs)
      {
        Assert.IsFalse(output.HasValue); // no output values yet
      }

      // Increase number of outputs further, reconfigure, and re-check
      node.mOutputCount.Value = 17;
      node.mSelectAction.Value = "ResendNothing";
      Assert.IsNull(node.mIdleValue); // no idle value by default
      Assert.IsFalse(node.mInput.HasValue); // no input value yet
      Assert.IsFalse(node.mSelectIndexInput.HasValue); // no input selected yet
      Assert.AreEqual(17, node.mOutputs.Count);
      foreach (var output in node.mOutputs)
      {
        Assert.IsFalse(output.HasValue); // no output values yet
      }

      // Reduce number of outputs to one and re-check
      node.mOutputCount.Value = 1;
      Assert.IsNull(node.mIdleValue); // no idle value by default
      Assert.IsFalse(node.mInput.HasValue); // no input value yet
      Assert.IsFalse(node.mSelectIndexInput.HasValue); // no input selected yet
      Assert.AreEqual(1, node.mOutputs.Count);
      foreach (var output in node.mOutputs)
      {
        Assert.IsFalse(output.HasValue); // no output values yet
      }
    }

    [Test]
    public void TestAllIdleTypes()
    {
      Assert.IsFalse(node.mOutputs[0].HasValue);
      Assert.IsFalse(node.mOutputs[1].HasValue);

      node.mSelectAction.Value = "ResendPrevious";
      node.mInput.Value = 42;
      node.Startup();

      node.mIdleValueType.Value = PortTypes.Bool;
      Assert.IsNotNull(node.mIdleValue);
      Assert.IsFalse(node.mIdleValue.HasValue);
      node.mIdleValue.Value = true;
      node.mSelectIndexInput.Value = 0;
      node.mInput.Value = false;
      Assert.AreEqual(false, node.mOutputs[0].Value);
      Assert.IsFalse(node.mOutputs[1].HasValue);
      node.mSelectIndexInput.Value = 1;
      Assert.AreEqual(true, node.mOutputs[0].Value);
      Assert.AreEqual(42, node.mOutputs[1].Value);

      node.mIdleValueType.Value = PortTypes.Integer;
      Assert.IsNotNull(node.mIdleValue);
      Assert.IsFalse(node.mIdleValue.HasValue);
      node.mIdleValue.Value = 2;
      node.mSelectIndexInput.Value = 0;
      node.mInput.Value = 99;
      Assert.AreEqual(99, node.mOutputs[0].Value);
      Assert.AreEqual( 2, node.mOutputs[1].Value);
      node.mSelectIndexInput.Value = 1;
      Assert.AreEqual( 2, node.mOutputs[0].Value);
      Assert.AreEqual(42, node.mOutputs[1].Value);

      node.mIdleValueType.Value = PortTypes.Number;
      Assert.IsNotNull(node.mIdleValue);
      Assert.IsFalse(node.mIdleValue.HasValue);
      node.mIdleValue.Value = -1234.5;
      node.mSelectIndexInput.Value = 0;
      node.mInput.Value = 4711.12;
      Assert.AreEqual(4711.12, node.mOutputs[0].Value);
      Assert.AreEqual(-1234.5, node.mOutputs[1].Value);
      node.mSelectIndexInput.Value = 1;
      Assert.AreEqual(-1234.5, node.mOutputs[0].Value);
      Assert.AreEqual(42, node.mOutputs[1].Value);

      node.mIdleValueType.Value = PortTypes.TimeSpan;
      Assert.IsNotNull(node.mIdleValue);
      Assert.IsFalse(node.mIdleValue.HasValue);
      node.mIdleValue.Value = new TimeSpan(1 /* hour */, 0, 0);
      node.mSelectIndexInput.Value = 0;
      node.mInput.Value = new TimeSpan(6 /* hours */, 0, 0);
      Assert.AreEqual(new TimeSpan(6, 0, 0), node.mOutputs[0].Value);
      Assert.AreEqual(new TimeSpan(1, 0, 0), node.mOutputs[1].Value);
      node.mSelectIndexInput.Value = 1;
      Assert.AreEqual(new TimeSpan(1, 0, 0), node.mOutputs[0].Value);
      Assert.AreEqual(42, node.mOutputs[1].Value);

      node.mIdleValueType.Value = PortTypes.Time;
      Assert.IsNotNull(node.mIdleValue);
      Assert.IsFalse(node.mIdleValue.HasValue);
      node.mIdleValue.Value = new TimeSpan(8 /* hour */, 30 /* min */, 0);
      node.mSelectIndexInput.Value = 0;
      node.mInput.Value = new TimeSpan(17 /* hours */, 45 /* min */, 0);
      Assert.AreEqual(new TimeSpan(17, 45, 0), node.mOutputs[0].Value);
      Assert.AreEqual(new TimeSpan( 8, 30, 0), node.mOutputs[1].Value);
      node.mSelectIndexInput.Value = 1;
      Assert.AreEqual(new TimeSpan( 8, 30, 0), node.mOutputs[0].Value);
      Assert.AreEqual(42, node.mOutputs[1].Value);

      node.mIdleValueType.Value = PortTypes.Date;
      Assert.IsNotNull(node.mIdleValue);
      Assert.IsFalse(node.mIdleValue.HasValue);
      node.mIdleValue.Value = new DateTime(2018, 12, 31);
      node.mSelectIndexInput.Value = 0;
      node.mInput.Value = new DateTime(2019, 1, 17);
      Assert.AreEqual(new DateTime(2019, 1, 17), node.mOutputs[0].Value);
      Assert.AreEqual(new DateTime(2018, 12, 31), node.mOutputs[1].Value);
      node.mSelectIndexInput.Value = 1;
      Assert.AreEqual(new DateTime(2018, 12, 31), node.mOutputs[0].Value);
      Assert.AreEqual(42, node.mOutputs[1].Value);

      node.mIdleValueType.Value = PortTypes.DateTime;
      Assert.IsNotNull(node.mIdleValue);
      Assert.IsFalse(node.mIdleValue.HasValue);
      node.mIdleValue.Value = new DateTime(2018, 12, 31, 23, 59, 59);
      node.mSelectIndexInput.Value = 0;
      node.mInput.Value = new DateTime(2019, 1, 17, 8, 0, 0);
      Assert.AreEqual(new DateTime(2019, 1, 17, 8, 0, 0), node.mOutputs[0].Value);
      Assert.AreEqual(new DateTime(2018, 12, 31, 23, 59, 59), node.mOutputs[1].Value);
      node.mSelectIndexInput.Value = 1;
      Assert.AreEqual(new DateTime(2018, 12, 31, 23, 59, 59), node.mOutputs[0].Value);
      Assert.AreEqual(42, node.mOutputs[1].Value);

      node.mIdleValueType.Value = PortTypes.String;
      Assert.IsNotNull(node.mIdleValue);
      Assert.IsFalse(node.mIdleValue.HasValue);
      node.mIdleValue.Value = "jumps over the lazy dog";
      node.mSelectIndexInput.Value = 0;
      node.mInput.Value = "the quick brown fox";
      Assert.AreEqual("the quick brown fox", node.mOutputs[0].Value);
      Assert.AreEqual("jumps over the lazy dog", node.mOutputs[1].Value);
      node.mSelectIndexInput.Value = 1;
      Assert.AreEqual("jumps over the lazy dog", node.mOutputs[0].Value);
      Assert.AreEqual(42, node.mOutputs[1].Value);
    }

    [Test]
    public void TestLockOut()
    {
      node.mOutputCount.Value = 1;
      Assert.AreEqual(1, node.mOutputs.Count);

      // Configure to NOT re-send upon select
      node.mSelectAction.Value = "ResendNothing";

      // Send input value without selecting the output before
      node.mInput.Value = 3;
      Assert.IsFalse(node.mOutputs[0].HasValue); // no output value yet

      // Select output, expect no change
      node.mSelectIndexInput.Value = 0;
      Assert.IsFalse(node.mOutputs[0].HasValue); // no output value yet

      // Send input value, expect it to be forwarded
      node.mInput.Value = 5;
      Assert.AreEqual(5, node.mOutputs[0].Value);

      // Change input value, expect it to be forwarded
      node.mInput.Value = -7.5;
      Assert.AreEqual(-7.5, node.mOutputs[0].Value);

      // De-select output, expect no change
      node.mSelectIndexInput.Value = -1;
      Assert.AreEqual(-7.5, node.mOutputs[0].Value);

      // Send yet another two input values, expect no change
      node.mInput.Value = 9.5;
      Assert.AreEqual(-7.5, node.mOutputs[0].Value);
      node.mInput.Value = -13;
      Assert.AreEqual(-7.5, node.mOutputs[0].Value);

      // Select output, expect no change
      node.mSelectIndexInput.Value = 0;
      Assert.AreEqual(-7.5, node.mOutputs[0].Value);

      // Reconfigure to re-send upon select and send an idle value upon
      //  de-select. With that, repeat all the same actions.
      node.mSelectAction.Value = "ResendPrevious";
      node.mIdleValueType.Value = PortTypes.Number;
      node.mIdleValue.Value = 1;

      // De-select output, send input value, expect idle value
      node.mSelectIndexInput.Value = 1;
      node.mInput.Value = 3;
      Assert.AreEqual(1, node.mOutputs[0].Value);

      // Select output, expect last value before de-select to be resent
      node.mSelectIndexInput.Value = 0;
      Assert.AreEqual(-7.5, node.mOutputs[0].Value);

      // Send input value, expect it to be forwarded
      node.mInput.Value = 5;
      Assert.AreEqual(5, node.mOutputs[0].Value);

      // Change input value, expect it to be forwarded
      node.mInput.Value = 5.5;
      Assert.AreEqual(5.5, node.mOutputs[0].Value);

      // De-select output, expect idle value
      node.mSelectIndexInput.Value = -1;
      Assert.AreEqual(1, node.mOutputs[0].Value);

      // Send yet another two input values, expect no change
      node.mInput.Value = 9.5;
      Assert.AreEqual(1, node.mOutputs[0].Value);
      node.mInput.Value = -13;
      Assert.AreEqual(1, node.mOutputs[0].Value);

      // Select output, expect last value before de-select to be resent
      node.mSelectIndexInput.Value = 0;
      Assert.AreEqual(5.5, node.mOutputs[0].Value);
    }

    [Test]
    public void TestNoIdleNoResend()
    {
      node.mOutputCount.Value = 11;
      node.mSelectAction.Value = "ResendNothing";

      // Select each output and check outputs
      for (int i = 0; i < node.mOutputs.Count; i++)
      {
        node.mSelectIndexInput.Value = i;
        Assert.IsFalse(node.mOutputs[i].HasValue); // no output values yet
      }
      for (int i = 0; i < node.mOutputs.Count; i++)
      {
        Assert.IsFalse(node.mOutputs[i].HasValue); // no output values yet
      }

      // Send values to input and check again
      for (int i = 0; i < node.mOutputs.Count - 1; i++)
      {
        node.mInput.Value = 3 * i;
        // no output values forwarded to deselected outputs
        Assert.IsFalse(node.mOutputs[i].HasValue);
        // new output value on selected output
        Assert.AreEqual(3 * i, node.mOutputs[node.mOutputs.Count - 1].Value);
      }

      // Select a different output, check output values are unchanged 
      node.mSelectIndexInput.Value = 3;
      for (int i = 0; i < node.mOutputs.Count - 1; i++)
      {
        Assert.IsFalse(node.mOutputs[i].HasValue);
      }
      Assert.AreEqual(3 * (node.mOutputs.Count - 2),
            node.mOutputs[(node.mOutputs.Count - 1)].Value);

      // Send a value, check output values again
      node.mInput.Value = -4711.5;
      for (int i = 0; i < node.mOutputs.Count - 1; i++)
      {
        if (3 == i)
        {
          Assert.AreEqual(-4711.5, node.mOutputs[i].Value);
        }
        else
        {
          Assert.IsFalse(node.mOutputs[i].HasValue);
        }
      }
      Assert.AreEqual(3 * (node.mOutputs.Count - 2),
            node.mOutputs[(node.mOutputs.Count - 1)].Value);
    }

    [Test]
    public void TestIdleNoResend()
    {
      node.mOutputCount.Value = 11;
      node.mSelectAction.Value = "ResendNothing";
      node.mIdleValueType.Value = PortTypes.Number;
      node.mIdleValue.Value = -100;

      // Startup and check outputs
      node.Startup();
      for (int i = 0; i < node.mOutputs.Count; i++)
      {
        Assert.AreEqual(-100, node.mOutputs[i].Value); // idle
      }

      // Select each output and check outputs again
      for (int i = 0; i < node.mOutputs.Count; i++)
      {
        node.mSelectIndexInput.Value = i;
        Assert.AreEqual(-100, node.mOutputs[i].Value); // idle
      }

      // Send values to input and check again
      for (int i = 0; i < node.mOutputs.Count - 1; i++)
      {
        node.mInput.Value = 3 * i;
        // idle value forwarded to deselected outputs
        Assert.AreEqual(-100, node.mOutputs[i].Value);
        // new output value on selected output
        Assert.AreEqual(3 * i, node.mOutputs[node.mOutputs.Count - 1].Value);
      }

      // Select a different output, check output values are unchanged 
      node.mSelectIndexInput.Value = 3;
      for (int i = 0; i < node.mOutputs.Count; i++)
      {
        Assert.AreEqual(-100, node.mOutputs[i].Value);
      }

      // Send a value, check output values again
      node.mInput.Value = -4711.5;
      for (int i = 0; i < node.mOutputs.Count; i++)
      {
        if (3 == i)
        {
          Assert.AreEqual(-4711.5, node.mOutputs[i].Value);
        }
        else
        {
          Assert.AreEqual(-100, node.mOutputs[i].Value);
        }
      }

      // Select a previously selected output, send no value, and check
      // outputs again
      node.mSelectIndexInput.Value = node.mOutputs.Count - 1;
      for (int i = 0; i < node.mOutputs.Count; i++)
      {
        Assert.AreEqual(-100, node.mOutputs[i].Value);
      }
    }

    [Test]
    public void TestIdleResendCurrent()
    {
      node.mOutputCount.Value = 7;
      node.mSelectAction.Value = "ResendCurrent";
      node.mIdleValueType.Value = PortTypes.Number;
      node.mIdleValue.Value = -100;

      // Select each output and check outputs
      for (int i = 0; i < node.mOutputs.Count; i++)
      {
        node.mSelectIndexInput.Value = i;
        Assert.IsFalse(node.mOutputs[i].HasValue); // no output values yet
      }
      for (int i = 0; i < node.mOutputs.Count - 1; i++)
      { // deselected ==> idle
        Assert.AreEqual(-100, node.mOutputs[i].Value);
      }
      // Never deselected, no output value yet
      Assert.IsFalse(node.mOutputs[node.mOutputs.Count - 1].HasValue);

      // Send values to input and check again
      for (int i = 0; i < node.mOutputs.Count - 1; i++)
      {
        node.mInput.Value = 3 * i;
        // idle value forwarded to deselected outputs
        Assert.AreEqual(-100, node.mOutputs[i].Value);
        // new output value on selected output
        Assert.AreEqual(3 * i, node.mOutputs[node.mOutputs.Count - 1].Value);
      }

      // Select a different output, check output values are unchanged 
      node.mSelectIndexInput.Value = 3;
      for (int i = 0; i < node.mOutputs.Count; i++)
      {
        if ( 3 == i )
        {
          Assert.AreEqual(15, node.mOutputs[i].Value);
        }
        else
        {
          Assert.AreEqual(-100, node.mOutputs[i].Value);
        }
      }

      // Send a value, check output values again
      node.mInput.Value = -4711.5;
      for (int i = 0; i < node.mOutputs.Count; i++)
      {
        if (3 == i)
        {
          Assert.AreEqual(-4711.5, node.mOutputs[i].Value);
        }
        else
        {
          Assert.AreEqual(-100, node.mOutputs[i].Value);
        }
      }

      // Select a previously selected output, send no value, and check
      // outputs again
      node.mSelectIndexInput.Value = node.mOutputs.Count - 1;
      for (int i = 0; i < node.mOutputs.Count - 1; i++)
      {
        Assert.AreEqual(-100, node.mOutputs[i].Value);
      }
      Assert.AreEqual(-4711.5, node.mOutputs[(node.mOutputs.Count - 1)].Value);
    }

    [Test]
    public void TestIdleResendPrevious()
    {
      node.mOutputCount.Value = 7;
      node.mSelectAction.Value = "ResendPrevious";
      node.mIdleValueType.Value = PortTypes.Number;
      node.mIdleValue.Value = -100;

      // Select each output and check outputs
      for (int i = 0; i < node.mOutputs.Count; i++)
      {
        node.mSelectIndexInput.Value = i;
        Assert.IsFalse(node.mOutputs[i].HasValue); // no output values yet
      }
      for (int i = 0; i < node.mOutputs.Count - 1; i++)
      { // deselected ==> idle
        Assert.AreEqual(-100, node.mOutputs[i].Value);
      }
      // Never deselected, no output value yet
      Assert.IsFalse(node.mOutputs[node.mOutputs.Count - 1].HasValue);

      // Send values to input and check again
      for (int i = 0; i < node.mOutputs.Count - 1; i++)
      {
        node.mInput.Value = 3 * i;
        // idle value forwarded to deselected outputs
        Assert.AreEqual(-100, node.mOutputs[i].Value);
        // new output value on selected output
        Assert.AreEqual(3 * i, node.mOutputs[node.mOutputs.Count - 1].Value);
      }

      // Select a different output, check output values are unchanged 
      node.mSelectIndexInput.Value = 3;
      for (int i = 0; i < node.mOutputs.Count; i++)
      {
        Assert.AreEqual(-100, node.mOutputs[i].Value);
      }

      // Send a value, check output values again
      node.mInput.Value = -4711.5;
      for (int i = 0; i < node.mOutputs.Count; i++)
      {
        if (3 == i)
        {
          Assert.AreEqual(-4711.5, node.mOutputs[i].Value);
        }
        else
        {
          Assert.AreEqual(-100, node.mOutputs[i].Value);
        }
      }

      // Select a previously selected output, send no value, and check
      // outputs again
      node.mSelectIndexInput.Value = node.mOutputs.Count - 1;
      for (int i = 0; i < node.mOutputs.Count - 1; i++)
      {
        Assert.AreEqual(-100, node.mOutputs[i].Value);
      }
      Assert.AreEqual(3 * (node.mOutputs.Count - 2),
            node.mOutputs[(node.mOutputs.Count - 1)].Value);
    }
  }
}
