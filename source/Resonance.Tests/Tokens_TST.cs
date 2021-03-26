using Microsoft.VisualStudio.TestTools.UnitTesting;
using Resonance.Tests.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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
    }
}
