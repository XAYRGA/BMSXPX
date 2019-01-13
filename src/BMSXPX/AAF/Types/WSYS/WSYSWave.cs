using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMSXPX.AAF.Types
{
    public class WSYSWave
    {
        public uint id;
        public ushort format;
        public ushort key;
        public double sampleRate;
        public uint sampleCount; 


        public uint w_start;
        public uint w_size;

        public bool loop;
        public uint loop_start;
        public uint loop_end;


        public string pcmpath;

    }
}
