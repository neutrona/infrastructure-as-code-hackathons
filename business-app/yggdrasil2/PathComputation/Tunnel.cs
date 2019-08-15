using System;
using System.Collections.Generic;
using System.Text;

namespace shift.yggdrasil2.PathComputation
{
    class Tunnel
    {
        public List<Path> Paths { get; set; }

        public Tunnel()
        {
            this.Paths = new List<Path>();
        }
    }
}
