using Microsoft.VisualStudio.TestTools.UnitTesting;
using Resonance.Tests.Common;
using Resonance.Tests.Common.Messages;
using Resonance.Threading;
using Resonance.Tokens;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Tests
{
    [TestClass]
    [TestCategory("Tokens")]
    public class Tokens_TST : ResonanceTest
    {
        [TestMethod]
        public void Short_Token_Million_Unique_Tokens_And_Performant()
        {
            Init();

            var generator = new Tokens.ShortGuidGenerator();

            HashSet<String> hashSet = new HashSet<string>();

            List<double> measurements = new List<double>();

            Stopwatch watch = new Stopwatch();

            for (int i = 0; i < 10000000; i++)
            {
                watch.Restart();
                var token = generator.GenerateToken(new object());
                measurements.Add(watch.ElapsedMilliseconds);
                Assert.IsTrue(hashSet.Add(token), "None unique token produced.");
            }

            //Now check that 95 percent of tokens were generated in under 1 millisecond.
            int underMiliTokensCount = measurements.Count(x => x <= 1);

            Assert.IsTrue((double)underMiliTokensCount / measurements.Count * 100d >= 95);
        }

        [TestMethod]
        public void Sequantial_Token()
        {
            ConcurrentList<int> tokens = new ConcurrentList<int>();

            var t1 = Task.Run(() =>
            {
                var generator = new SequentialTokenGenerator();

                for (int i = 0; i < 1000; i++)
                {
                    tokens.Add(int.Parse(generator.GenerateToken(null)));
                }
            });

            var t2 = Task.Run(() =>
            {
                var generator = new SequentialTokenGenerator();

                for (int i = 0; i < 1000; i++)
                {
                    tokens.Add(int.Parse(generator.GenerateToken(null)));
                }
            });

            Task.WaitAll(t1, t2);

            List<int> tokensOrdered = tokens.OrderBy(x => x).ToList();

            for (int i = 0; i < tokensOrdered.Count; i++)
            {
                Assert.AreEqual(tokensOrdered[i], i + 1);
            }
        }

        [TestMethod]
        public void HashCode_Token()
        {
            HashSet<String> tokens = new HashSet<String>();

            var generator = new HashCodeTokenGenerator();

            int max = 1000;

            for (int i = 0; i < max; i++)
            {
                String token = generator.GenerateToken(new CalculateRequest() { A = i });
                Assert.IsTrue(tokens.Add(token), $"HashCode token failed on token {i + 1} out of {max}.");
            }

            for (int i = 0; i < max; i++)
            {
                String token = generator.GenerateToken(new UniqueHashCodeObject() { A = i });
                Assert.IsTrue(tokens.Add(token), $"HashCode token failed on token {i + 1} out of {max}.");
            }
        }

        private class UniqueHashCodeObject
        {
            public int A { get; set; }

            public override int GetHashCode()
            {
                return A;
            }
        }
    }
}
