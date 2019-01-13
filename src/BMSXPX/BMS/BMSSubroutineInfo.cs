using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Be.IO; 

namespace BMSXPX.BMS
{
    class BMSSubroutineInfo
    {   // This is used to preload all of the routines so that way we know what resources we have to load. 

        public struct BMSSubroutineInstrumentInstance // pretty lonk dont you think? 
        {
           public int bank;
           public int inst;
        }
        public uint start;
        public int id;
        public int end;
        public int size;
        public byte[] buffer;
        public int ppqn;
        public int bpm;

        private int CurrentBank = 0;
        private int CurrentInst = 0;
        private bool PrgmUpdateSinceLastNote = false; 

 

        BinaryReader breader;

        public BMSSubroutineInstrumentInstance[] usedInstruments;
        public int usedInstCount; 
        

        public void count(int amt)
        {
            breader.BaseStream.Seek(amt, SeekOrigin.Current);
        }


        /* 
         * Credit to jasper for the enums. 
        Names from JASystem::TSeqParser from framework.map in The Wind Waker. 
        enum MML
        {
            /* wait with u8 arg 
            MML_WAIT_8 = 0x80,
            /* wait with u16 arg 
            MML_WAIT_16 = 0x88,
            /* wait with variable-length arg 
            MML_WAIT_VAR = 0xF0,

            /* Perf is hard to enum, but here goes: 
            MML_PERF_U8_NODUR = 0x94,
            MML_PERF_U8_DUR_U8 = 0x96,
            MML_PERF_U8_DUR_U16 = 0x97,
            MML_PERF_S8_NODUR = 0x98,
            MML_PERF_S8_DUR_U8 = 0x9A,
            MML_PERF_S8_DUR_U16 = 0x9B,
            MML_PERF_S16_NODUR = 0x9C,
            MML_PERF_S16_DUR_U8 = 0x9E,
            MML_PERF_S16_DUR_U16 = 0x9F,

            MML_PARAM_SET_8 = 0xA4,
            MML_PARAM_SET_16 = 0xAC,

            MML_OPEN_TRACK = 0xC1,
            /* open sibling track, seems unused 
            MML_OPEN_TRACK_BROS = 0xC2,
            MML_CALL = 0xC3,
            MML_CALL_COND = 0xC4,
            MML_RET = 0xC5,
            MML_RET_COND = 0xC6,
            MML_JUMP = 0xC7,
            MML_JUMP_COND = 0xC8,
            MML_TIME_BASE = 0xFD,
            MML_TEMPO = 0xFE,
            MML_FIN = 0xFF,

            /* "Improved" JaiSeq from TP / SMG / SMG2 seems to use this instead 
            MML2_SET_PERF_8 = 0xB8,
            MML2_SET_PERF_16 = 0xB9,
            /* Set "articulation"? Used for setting timebase. 
            MML2_SET_ARTIC = 0xD8,
            MML2_TEMPO = 0xE0,
            MML2_SET_BANK = 0xE2,
            MML2_SET_PROG = 0xE3,
        };

        */

        public int nextOpSize()
        {
            var opcode = breader.ReadByte();
         
            
            if (opcode < 0x80) {

                count(2);

                if (PrgmUpdateSinceLastNote==true)
                {
                    usedInstruments[usedInstCount] = new BMSSubroutineInstrumentInstance()
                    {
                        bank = CurrentBank,
                        inst = CurrentInst

                    };
                    usedInstCount++;
                }

            }

            else if (opcode == 0x80) { count(1); }
            else if (opcode < 0x88) {  }
            else if (opcode == 0x88) { count(2); }
            else if (opcode == 0xA0) { count(2); }
            else if (opcode == 0x98) { count(2); }
            else if (opcode == 0x9A) { count(3);  }
            else if (opcode == 0x9C) { count(3); }
            else if (opcode == 0x9E) { count(4); }
          
            else if (opcode == 0xA3) { count(2); }
            else if (opcode == 0xA4)
            {


                var type = (int)breader.ReadByte();
                var value = (int)breader.ReadByte();
                if (type == 0x20)
                {
                    CurrentBank = value;
                    PrgmUpdateSinceLastNote = true; 

                }
                else if (type == 0x21)
                {
                    CurrentInst = value;
                    PrgmUpdateSinceLastNote = true; 
                }




            }
            else if (opcode == 0xA5) { count(2); }
            else if (opcode == 0xA7) { count(2); }
            else if (opcode == 0xA9) { count(4); }
            else if (opcode == 0xAA) { count(4); }
            else if (opcode == 0xAC) { count(3); }
            else if (opcode == 0xAD) { count(3); }
            else if (opcode == 0xB1) {
                int flag = breader.ReadByte(); 
                if (flag==0x40) { count(2);  }
                if (flag==0x80) { count(4); }
            }
            else if (opcode == 0xC1)
            {
                Console.WriteLine("[!] Subroutine inside of a subroutine! 0x{0:X}", breader.BaseStream.Position);
                return 3;
            }
            else if (opcode == 0xB8) { count(2); }
            else if (opcode == 0xC2) { count(1); }
            else if (opcode == 0xB4) { count(4); }
            else if (opcode == 0xC6) { count(1); }
            else if (opcode == 0xC7) { count(4); }
            else if (opcode == 0xC8) { count(4); }
            else if (opcode == 0xCB) { count(2); }
            else if (opcode == 0xCC) { count(2); }
            else if (opcode == 0xCF) { count(1); }
            else if (opcode == 0xD0) { count(2); }
            else if (opcode == 0xD1) { count(2); }
            else if (opcode == 0xD2) { count(2); }
            else if (opcode == 0xD5) { count(2); }
            else if (opcode == 0xD8) { count(2); }
            else if (opcode == 0xDA) { count(1); }
            else if (opcode == 0xDB) { count(1); }
            else if (opcode == 0xDD) { count(3); }
            else if (opcode == 0xDF) { count(4); }
            else if (opcode == 0xE0) { count(2); }
            else if (opcode == 0xE2) { count(1); }
            else if (opcode == 0xE3) { count(1); }
            else if (opcode == 0xE6) { count(2); }
            else if (opcode == 0xE7) { count(2); }
            else if (opcode == 0xEF) { count(3); }
            ///////////////////////////////////////
            else if (opcode== 0xF0)
            {
                
                int fade = (int)breader.ReadByte(); 
                while ((fade & 0x80) > 0 )
                {
                    fade = ((fade & 0x7F) << 7);
                    fade += breader.ReadByte();


                }
                
               
            }
            else if (opcode == 0xF1) { count(1); }
            else if (opcode == 0xF4) { count(1); }
            else if (opcode == 0xF9) { count(2); }
            else if (opcode == 0xFD) {
                var trate = breader.ReadInt16();
                if (bpm == 0)
                {
                    Console.WriteLine("Got Initial BPM {0}", trate);
                    bpm = trate;
                }
            }
            else if (opcode == 0xFE) {
                var trate = breader.ReadInt16();
                if (ppqn == 0)
                {
                    Console.WriteLine("Got Initial PPQN {0}", trate);
                    ppqn = trate;
                }
                
            }
            else if (opcode == 0xFF)
            {
                return 0;

            } else
            {
                Console.WriteLine("Unknown opcode 0x{0:X} @ 0x{1:X}", opcode, breader.BaseStream.Position);
                return -1; 
            }




            return 1; 
        }

        public BMSSubroutineInfo(int id, uint offset, BinaryReader reader)
        {
            var asd = 0;
            breader = reader;
            breader.BaseStream.Seek(offset, SeekOrigin.Begin);

            usedInstruments = new BMSSubroutineInstrumentInstance[0xFFFF]; // Shouldnt change instrument more than 256 times per song. 


            while (true)
            {
                asd = nextOpSize(); 
                if (asd==0)
                {
                    break;
                }

            }
            size = (int)(breader.BaseStream.Position - offset);
     

            if (id==100)
            {
                Console.WriteLine("Cycle length should be {0}ms", (60000/(float)bpm)/(float)ppqn );
            }
            
            
            Console.WriteLine("Done, ends at 0x{0:X} (Size 0x{1:X})",  size + offset,size  );
        }



    }
}
