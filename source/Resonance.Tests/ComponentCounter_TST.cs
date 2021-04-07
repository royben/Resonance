using Microsoft.VisualStudio.TestTools.UnitTesting;
using Resonance.Tests.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Tests
{
    [TestClass]
    [TestCategory("Component Counter Manager")]
    public class ComponentCounter_TST : ResonanceTest, IResonanceComponent
    {
        [TestMethod]
        public void Component_Counter_Manager()
        {
            Init();

            List<int> counters = new List<int>();

            var t1 = Task.Factory.StartNew(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    counters.Add(ResonanceComponentCounterManager.Default.GetIncrement(this));
                    Thread.Sleep(1);
                }
            });

            var t2 = Task.Factory.StartNew(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    counters.Add(ResonanceComponentCounterManager.Default.GetIncrement(this));
                    Thread.Sleep(1);
                }
            });

            Task.WaitAll(t1, t2);

            counters = counters.OrderBy(x => x).ToList();

            Assert.IsTrue(counters.Count == 200);

            int last = 0;

            for (int i = 0; i < counters.Count; i++)
            {
                Assert.AreEqual(last + 1, counters[i]);
                last = counters[i];
            }
        }
    }
}
