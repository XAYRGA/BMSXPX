using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMSXPX.AAF.Types
{
    public class WSYSGroup
    {
        public string filename;
        public Dictionary<uint,WSYSWave> waves; 

        public WSYSGroup(uint count)
        {
            waves = new Dictionary<uint, WSYSWave>();


        }

        
    }
}
