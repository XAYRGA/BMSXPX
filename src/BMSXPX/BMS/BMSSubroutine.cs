using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Be.IO;
using System.IO;
using BMSXPX.AAF;


namespace BMSXPX.BMS
{
    class BMSSubroutine
    {
        byte[] BMSData;

        BeBinaryReader RoutineReader;

        Stack<uint> AddrStack;

        public int Instrument;

        public int Bank; 

        public int Delay;

        public int size;

        public int baseaddr;

        public int idx;

        public bool stopped;

        public byte[] current_inst_pcmdata;


        public BMSSubroutine(int id, int start, int end, ref byte[] BData, AAFFile data)
        {
            size = start - end; // Routine size 

            idx = id;

            baseaddr = start;

            Console.WriteLine("0x{0:X}", baseaddr);

            BMSData = BData; // Copy data reference into the current class. 

            RoutineReader = new BeBinaryReader(new MemoryStream(BMSData)); // Create an independent binary reader for it

            RoutineReader.BaseStream.Seek(start, 0); // Seek to the routine start position.

            AddrStack =  new Stack<uint>(16); // Create the call / jump stack. 

            Instrument = 0; // Setup current inst. 

            Bank = -1; 

            stopped = false;

        }

        public void PushStackAddr(uint newaddr)
        {
            AddrStack.Push(newaddr);
        }

        public uint PopStackAddr()
        {
            return AddrStack.Pop(); 
        }

        public void AddDelay(int Del)
        {
            Delay += Del;
        }

        public void JumpTo(uint addr)
        {
            RoutineReader.BaseStream.Seek(addr,0); 
        }
   
        public byte getOpcode()
        {
            return RoutineReader.ReadByte();
        }
        public ushort getOpword()
        {
            return RoutineReader.ReadUInt16();
        }

        public uint readUInt24BE()
        {
            try
            {
                var b1 = getOpcode();
                var b2 = getOpcode();
                var b3 = getOpcode();
                return
                    (((uint)b1) << 16) |
                    (((uint)b2) << 8) |
                    ((uint)b3);
            }
            catch
            {
                return 0u;
            }
        }

        public int readVlq()
        {
            int fade = (int)getOpcode() ;
            while ((fade & 0x80) > 0)
            {
                fade = ((fade & 0x7F) << 7);
                fade += getOpcode();


            }
            return fade;
        }

        public void skip(int count)
        {
            RoutineReader.BaseStream.Seek(count, SeekOrigin.Current);
        }

        public bool reloadInstruments()
        {
            if (Bank==-1)
            {
                Console.WriteLine("NOT LOADING INSTRUMENTS. Bank isn't defined yet");
                return false; 
            }

            return true;
        }
    }
}
