using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMSXPX.AAF
{
    public struct AAFChunk
    {
        public uint id;
        public uint offset;
        public uint size;
        public uint type;
        public bool subchunk;
        public uint rootchk;
    }
}
