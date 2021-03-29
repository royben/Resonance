using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Messages
{
    public class ProgressRequest
    {
        public int Count { get; set; }
        public TimeSpan Interval { get; set; }
    }
}
