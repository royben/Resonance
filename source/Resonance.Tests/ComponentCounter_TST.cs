using Microsoft.VisualStudio.TestTools.UnitTesting;
using Resonance.Tests.Common;
using System;
using System.Collections.Concurrent;
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

            ResonanceComponentCounterManager.Default.Reset();

            // The subject under test is the counter manager, so the collection that
            // gathers the results must itself be thread-safe: List<T>.Add from two
            // threads can drop entries and would fail this test for the wrong reason.
            ConcurrentQueue<int> results = new ConcurrentQueue<int>();

            var t1 = Task.Factory.StartNew(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    results.Enqueue(ResonanceComponentCounterManager.Default.GetIncrement(this));
                    Thread.Sleep(1);
                }
            });

            var t2 = Task.Factory.StartNew(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    results.Enqueue(ResonanceComponentCounterManager.Default.GetIncrement(this));
                    Thread.Sleep(1);
                }
            });

            Task.WaitAll(t1, t2);

            List<int> counters = results.OrderBy(x => x).ToList();

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
