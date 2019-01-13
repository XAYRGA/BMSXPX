using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BMSXPX.BMS;
using BMSXPX.AAF; 

namespace BMSXPX
{
    static class BMSXP
    {
        static int Main(string[] args)
        {
            
            Console.WriteLine("BMSXPX by XayrGA\nJAudio Engine Emulator");
           // Console.ReadLine();
            var AAF = new AAFFile("JaiInit.aaf");
            var BMSF = new BMSFile("test.bms",AAF);

            Console.ReadLine();
            return 0;
        }
    }
}
