using System;
using LogicModule.Nodes.TestHelper;
using LogicModule.ObjectModel;
using NUnit.Framework;

namespace Recomedia_de.Logic.Generic.Test
{
  [TestFixture]
  public class SendByDifferenceTest
  {
    private INodeContext context;
    private SendByDifference node;

    public SendByDifferenceTest()
    {
      context = TestNodeContext.Create();
    }

    [SetUp]
    public void SendByDifferenceTestSetUp()
    {
      node = new SendByDifference(context);
    }

    [TearDown]
    public void SendByDifferenceTestTearDown()
    {
      node = null;
    }

    [Test]
    public void SendByDifferenceTestDoublesDefaultMinDiff()
    {
      // Check initial state
      Assert.IsTrue(node.mMinimumDifference.HasValue);
      Assert.AreEqual(1.0, node.mMinimumDifference.Value);
      Assert.IsFalse(node.mOutput.HasValue); // no output value

      // First set value must be forwarded
      node.mInput.Value = 42.73;
      node.Execute();
      Assert.IsTrue(node.mOutput.HasValue);
      Assert.AreEqual(42.73, node.mOutput.Value);

      // Further set values with at least 1.0 difference must be forwarded
      node.mInput.Value = 43.73;
      node.Execute();
      Assert.AreEqual(43.73, node.mOutput.Value);
      node.mInput.Value = 42.73;
      node.Execute();
      Assert.AreEqual(42.73, node.mOutput.Value);
      node.mInput.Value = 41.73;
      node.Execute();
      Assert.AreEqual(41.73, node.mOutput.Value);

      // Further set values with a smaller difference must NOT be forwarded
      node.mInput.Value = 42.72;
      node.Execute();
      Assert.AreEqual(41.73, node.mOutput.Value);
      node.mInput.Value = 40.74;
      node.Execute();
      Assert.AreEqual(41.73, node.mOutput.Value);
    }

    [Test]
    public void SendByDifferenceTestIntsSetMinDiff()
    {
      node.mMinimumDifference.Value = 3;
      node.Execute();
      Assert.IsFalse(node.mOutput.HasValue); // initially no output value

      // First set value must be forwarded
      node.mInput.Value = 17;
      node.Execute();
      Assert.AreEqual(17, node.mOutput.Value);

      // Further set values with a difference < 3 must NOT be forwarded
      node.mInput.Value = 15;
      node.Execute();
      Assert.AreEqual(17, node.mOutput.Value);
      node.mInput.Value = 19;
      node.Execute();
      Assert.AreEqual(17, node.mOutput.Value);

      // Further set values with at least 2.0 difference must be forwarded
      node.mInput.Value = 20;
      node.Execute();
      Assert.AreEqual(20, node.mOutput.Value);
      node.mInput.Value = 16;
      node.Execute();
      Assert.AreEqual(16, node.mOutput.Value);
    }
  }
}
