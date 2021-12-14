using System;
using System.Collections;
using System.Collections.Generic;
using LogicModule.ObjectModel;
using LogicModule.ObjectModel.TypeSystem;
using LogicModule.Nodes.TestHelper;
using NUnit.Framework;

namespace Recomedia_de.Logic.VisuWeb.Test
{
  [TestFixture]
  public class PlaceholderTestBase<NodeT> where NodeT: PlaceholderNodeBase
  {
    protected INodeContext context;
    protected NodeT node;

    public PlaceholderTestBase()
    {
      context = TestNodeContext.Create();
    }

    [TearDown]
    public void PlaceholderTestBaseTearDown()
    {
      node = null;
    }

    [Test]
    public void InitialState()
    {
      Assert.AreEqual(1, node.mTemplateCount.Value);

      Assert.IsNotNull(node.mTemplates);          // should be List ...
      Assert.AreEqual(1, node.mTemplates.Count);  // ... of length 1
      Assert.IsNotNull(node.mTemplates[0]);       // should be string
      Assert.IsFalse(node.mTemplates[0].HasValue);
      Assert.AreEqual("", node.mTemplates[0].Value);

      checkInputCounts(0, 0, 0, 0);
      Assert.IsNotNull(node.mOutputs[0]);         // should be double
      Assert.IsFalse(node.mOutputs[0].HasValue);  // no output value
    }

    protected void checkInputCounts(int expNumBinInputs,
                                    int expNumIntInputs,
                                    int expNumNumInputs,
                                    int expNumStrInputs)
    {
      Assert.AreEqual(expNumBinInputs, node.mBinInputs.Count);
      Assert.AreEqual(expNumIntInputs, node.mIntInputs.Count);
      Assert.AreEqual(expNumNumInputs, node.mNumInputs.Count);
      Assert.AreEqual(expNumStrInputs, node.mStrInputs.Count);
    }

    protected void checkInputNoValues()
    {
      foreach (var inp in node.mBinInputs)
      {
        Assert.IsNotNull(inp);
        Assert.IsFalse(inp.HasValue);
      }

      foreach (var inp in node.mIntInputs)
      {
        Assert.IsNotNull(inp);
        Assert.IsFalse(inp.HasValue);
      }

      foreach (var inp in node.mNumInputs)
      {
        Assert.IsNotNull(inp);
        Assert.IsFalse(inp.HasValue);
      }

      foreach (var inp in node.mStrInputs)
      {
        Assert.IsNotNull(inp);
        Assert.IsFalse(inp.HasValue);
      }
    }

    protected void assignDefaultInputValues()
    {
      foreach (var inp in node.mBinInputs)
      {
        inp.Value = false;
      }
      foreach (var inp in node.mIntInputs)
      {
        inp.Value = 0;
      }
      foreach (var inp in node.mNumInputs)
      {
        inp.Value = 0.0;
      }
      foreach (var inp in node.mStrInputs)
      {
        inp.Value = "";
      }
    }

    protected void checkInputNames<ValT>(List<string> inputNames, IList<ValT> inputs)
      where ValT : ValueObjectBase
    {
      Assert.AreEqual(inputNames.Count, inputs.Count);

      for (int i = 0; i < inputs.Count; i++)
      {
        Assert.AreEqual(inputNames[i], inputs[i].Name);
      }
    }

    protected void checkDeLocalizedList<T>(List<string> expectedNames,
                                               IList<T> actualList,
                           Func<string, string, string> localize)
      where T : IValueObject
    {
      Assert.AreEqual(expectedNames.Count, actualList.Count);

      for (int i = 0; i < actualList.Count; i++)
      {
        Assert.AreEqual(expectedNames[i], localize("de", actualList[i].Name));
      }
    }
  }
}
