using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance
{
    public class ResonanceContinuousRequestConfig : ResonanceRequestConfig
    {
        public TimeSpan? ContinuousTimeout { get; set; }
    }
}
