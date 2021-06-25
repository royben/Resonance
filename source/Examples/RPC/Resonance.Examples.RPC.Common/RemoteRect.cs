using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Resonance.Examples.RPC.Common
{
    public struct RemoteRect
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            RemoteRect r = (RemoteRect)obj;
            return r.X == X && r.Y == Y && r.Width == Width && r.Height == Height;
        }

        public override int GetHashCode()
        {
            int hashCode = 466501756;
            hashCode = hashCode * -1521134295 + X.GetHashCode();
            hashCode = hashCode * -1521134295 + Y.GetHashCode();
            hashCode = hashCode * -1521134295 + Width.GetHashCode();
            hashCode = hashCode * -1521134295 + Height.GetHashCode();
            return hashCode;
        }
    }
}
