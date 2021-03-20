using Resonance.Core.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Core
{
    public class ResonanceResponseConfig
    {
        public bool Completed { get; set; }
        public int? ErrorCode { get; set; }
        public String ErrorMessage { get; set; }
        public bool ShouldLog { get; set; }
        public QueuePriority Priority { get; set; }
    }
}
