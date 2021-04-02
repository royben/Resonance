using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
    }
}
