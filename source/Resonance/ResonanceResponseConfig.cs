using Resonance.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance
{
    public class ResonanceResponseConfig
    {
        public bool Completed { get; set; }
        public String ErrorMessage { get; set; }
        public bool HasError { get; set; }
        public bool ShouldLog { get; set; }
        public QueuePriority Priority { get; set; }
    }
}
