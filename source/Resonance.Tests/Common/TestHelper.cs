using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Tests.Common
{
    public static class TestHelper
    {
        public static List<double> GetOutliers(List<double> numbers)
        {
            List<double> normalNumbers = new List<double>();
            List<double> outLierNumbers = new List<double>();
            double avg = numbers.Average();
            double standardDeviation = Math.Sqrt(numbers.Average(v => Math.Pow(v - avg, 2)));
            foreach (double number in numbers)
            {
                if ((Math.Abs(number - avg)) > (2 * standardDeviation))
                    outLierNumbers.Add(number);
                else
                    normalNumbers.Add(number);
            }

            return outLierNumbers;
        }

        public static String GetSolutionFolder()
        {
            var path = Path.GetFullPath("../../../");
            return path;
        }

        public static byte[] GetRandomByteArray(int sizeInKb)
        {
            Random rnd = new Random();
            byte[] b = new byte[sizeInKb * 1024];
            rnd.NextBytes(b);
            return b;
        }

        public static void WaitWhile(Func<bool> func, TimeSpan timeout)
        {
            DateTime startTime = DateTime.Now;

            while (func())
            {
                Thread.Sleep(10);

                if (DateTime.Now > startTime + timeout)
                {
                    throw new TimeoutException("Could not complete the WaitWhile statement within the given timeout.");
                }
            }
        }
    }
}
