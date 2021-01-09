using System;
using LogicModule.Nodes.TestHelper;
using LogicModule.ObjectModel;
using NUnit.Framework;

namespace Recomedia_de.Logic.Generic.Test
{
  [TestFixture]
  public class InputSelectorTest
  {
    private INodeContext context;
    private InputSelector node;

    public InputSelectorTest()
    {
      context = TestNodeContext.Create();
    }

    [SetUp]
    public void InputSelectorTestSetUp()
    {
      node = new InputSelector(context);
    }

    [TearDown]
    public void InputSelectorTestTearDown()
    {
      node = null;
    }

    [Test]
    public void TestNumberOfInputs()
    {
      // Check initial state
      Assert.AreEqual(2, node.mInputCount.Value);
      Assert.AreEqual("ResendCurrent", node.mSelectAction.Value);
      foreach (var input in node.mInputs)
      {
        Assert.IsFalse(input.HasValue); // no input values
      }
      Assert.IsFalse(node.mOutput.HasValue); // no output value

      // Increase number of inputs and re-check
      node.mInputCount.Value = 7;
      Assert.AreEqual("ResendCurrent", node.mSelectAction.Value);
      foreach (var input in node.mInputs)
      {
        Assert.IsFalse(input.HasValue); // no input values
      }
      Assert.IsFalse(node.mOutput.HasValue); // no output value

      // Increase number of inputs further, reconfigure, and re-check
      node.mInputCount.Value = 17;
      node.mSelectAction.Value = "ResendNothing";

      // Check initial state
      foreach (var input in node.mInputs)
      {
        Assert.IsFalse(input.HasValue); // no input values
      }
      Assert.IsFalse(node.mOutput.HasValue); // no output value
    }

    [Test]
    public void TestWithoutResend()
    {
      node.mInputCount.Value = 5;
      node.mSelectAction.Value = "ResendNothing";

      // Select each input and check output
      for (int i = 0; i < node.mInputs.Count; i++)
      {
        node.mSelectIndexInput.Value = i;
        Assert.IsFalse(node.mOutput.HasValue); // no output value
      }

      // Send values to each but the selected (last) input and check again
      for (int i = 0; i < node.mInputs.Count - 1; i++)
      {
        node.mInputs[i].Value = 3 * i;
        Assert.IsFalse(node.mOutput.HasValue); // no output value
      }

      // Select a different input and do the same again
      node.mSelectIndexInput.Value = 3;
      for (int i = 0; i < node.mInputs.Count; i++)
      {
        if (3 != i)
        {
          node.mInputs[i].Value = 5 * i;
        }
        Assert.IsFalse(node.mOutput.HasValue); // no output value
      }

      // Send value to the selected input and check again
      node.mInputs[3].Value = 7 * 3;
      Assert.AreEqual(7 * 3, node.mOutput.Value);

      // Select each input and check again
      for (int i = 0; i < node.mInputs.Count; i++)
      {
        node.mSelectIndexInput.Value = i;
        Assert.AreEqual(7 * 3, node.mOutput.Value);
      }

      // Select each input, give it a new value, and check again
      for (int i = 0; i < node.mInputs.Count; i++)
      {
        node.mSelectIndexInput.Value = i;
        node.mInputs[i].Value = -2.5 * i;
        Assert.AreEqual(-2.5 * i, node.mOutput.Value);
      }
    }

    [Test]
    public void TestWithResend()
    {
      node.mInputCount.Value = 7;
      // Default is to re-send

      // Select each input and check output
      for (int i = 0; i < node.mInputs.Count; i++)
      {
        node.mSelectIndexInput.Value = i;
        Assert.IsFalse(node.mOutput.HasValue); // no output value
      }

      // Send values to each but the selected (last) input and check again
      for (int i = 0; i < node.mInputs.Count - 1; i++)
      {
        node.mInputs[i].Value = 3 * i;
        Assert.IsFalse(node.mOutput.HasValue); // no output value
      }

      // Select a the first input and do the same again
      node.mSelectIndexInput.Value = 0;
      Assert.AreEqual(0, node.mOutput.Value);   // due to re-send
      for (int i = 1; i < node.mInputs.Count; i++)
      {
        node.mInputs[i].Value = -5 * i;
        Assert.AreEqual(0, node.mOutput.Value);
      }

      // Send value to the selected (first) input and check again
      node.mInputs[0].Value = 4711.12;
      Assert.AreEqual(4711.12, node.mOutput.Value);

      // Select each input, check old value, give it a new value of different type,
      // and check again
      for (int i = 0; i < node.mInputs.Count; i++)
      {
        node.mSelectIndexInput.Value = i;
        if ( 0 == i )
        {
          Assert.AreEqual(4711.12, node.mOutput.Value);
        }
        else
        {
          Assert.AreEqual(-5 * i, node.mOutput.Value);
        }
        node.mInputs[i].Value = new DateTime(2019, 1, i + 1, 3 * i, 0, 0);
        Assert.AreEqual(new DateTime(2019, 1, i + 1, 3 * i, 0, 0), node.mOutput.Value);
      }
    }

    [Test]
    public void TestSelectOutOfRange()
    {
      node.mInputCount.Value = 3;

      // Select existing input and check output
      node.mSelectIndexInput.Value = 1;
      Assert.IsFalse(node.mOutput.HasValue); // no output value

      // Select non-existent input and check output
      int[] nonExInputs = { -1, 3 };
      foreach ( var input in nonExInputs )
      {
        node.mSelectIndexInput.Value = input;
        Assert.IsFalse(node.mOutput.HasValue); // no output value

        // Send values to each input and check again
        for (int i = 0; i < node.mInputs.Count; i++)
        {
          node.mInputs[i].Value = 3 * i;
          Assert.IsFalse(node.mOutput.HasValue); // no output value
        }
      }
    }

  }
}
