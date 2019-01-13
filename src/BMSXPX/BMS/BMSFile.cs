using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Be.IO;
using System.IO;
using System.Threading;
using System.Diagnostics;
using BMSXPX.AAF;

namespace BMSXPX.BMS
{
    class BMSFile
    {
        BMSSubroutineInfo MainSubInfo; 
        BMSSubroutineInfo[] SubsInfo;

        BMSSubroutine MainSeq;
        BMSSubroutine[] SeqArr;
        byte[] BMSData;
        Stream BMSF; 
        BeBinaryReader BReader;
        BMSSequencer Player;
        AAFFile AAFData;
        
        
        private uint ReadUInt24BE(BinaryReader reader)
        {
            try
            {
                var b1 = reader.ReadByte();
                var b2 = reader.ReadByte();
                var b3 = reader.ReadByte();
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


        public BMSFile(string file, AAFFile data)
        {
            SubsInfo = new BMSSubroutineInfo[64]; // Create the container for our subroutines
            SeqArr = new BMSSubroutine[64];

            AAFData = data; 

            BMSData = File.ReadAllBytes(file);
            BMSF = new MemoryStream(BMSData);

            BReader = new BeBinaryReader(BMSF); // Create the Big Endian binary reader for our data. 

           
            while (BReader.ReadByte() == 0xC1)
            { 
                // "Spawn Subroutine" opcode is 0xC1.  Will be at the end of our data all the time. 
                var subid = BReader.ReadByte();
                var offset = ReadUInt24BE(BReader);
                var oldpos = BReader.BaseStream.Position;

                Console.WriteLine("Subroutine {0} at {1:X} ", subid, offset);
                var sinf = new BMSSubroutineInfo(subid, offset, BReader);
                SubsInfo[subid] = sinf;
                sinf.start = offset;
                sinf.end = (int)offset + sinf.size;
                SeqArr[subid] = new BMSSubroutine(subid,(int)sinf.start, sinf.end, ref BMSData,data);
                BReader.BaseStream.Seek(oldpos, SeekOrigin.Begin);


            }

            var rawr = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[!] Next opcode is not a subroutine -- end routine loading. ");
            Console.ForegroundColor = rawr;

            BReader.BaseStream.Seek(-1, SeekOrigin.Current); // We read an extra byte with the while loop. 
            Console.WriteLine("Loading main routine from {0:X}", BReader.BaseStream.Position);
            MainSubInfo = new BMSSubroutineInfo(100, (uint)BReader.BaseStream.Position, BReader);
            MainSeq  = new BMSSubroutine(100,(int)MainSubInfo.start, MainSubInfo.end, ref BMSData,data);

            rawr = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("This language is capable of {0} ticks / second",Stopwatch.Frequency);
            Console.WriteLine("BMSXPX Is loading sample data please wait . . . .");
            // Thread.Sleep(3000);
            Console.ForegroundColor = rawr;

            Player = new BMSSequencer(MainSubInfo, MainSeq, SeqArr,data,SubsInfo);
            Player.StartPlayback();

        }

    }
}
