using System;
using System.Collections.Generic;
using System.Text;

namespace shift.yggdrasil2.PathComputation
{
    class Path
    {
        public List<Topology.IGP.Link.Link> Hops { get; set; }

        public bool Discard { get; set; }

        public List<long> UnderlayIDs { get; set; }
        public int UnderlayDiversityIndex { get; set; }

        public double ComputedRTT { get; set; }

        public Path()
        {
            this.Hops = new List<Topology.IGP.Link.Link>();
            this.Discard = false;
            this.UnderlayIDs = new List<long>();
            this.UnderlayDiversityIndex = 0;
            this.ComputedRTT = 0;
        }
    }
}
