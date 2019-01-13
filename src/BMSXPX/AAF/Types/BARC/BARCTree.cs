using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO; 
using Be.IO; 

namespace BMSXPX.AAF.Types
{
    struct BARCLocation
    {
        public string file;
        public uint index;
        public uint offset;
        public uint size;

    }


    class BARCTree
    {
        /* 
          
          0x00 + 8 'BARC----' (Magic 0x2D2D2D2D42415243)
          0x08 uint32 ?? -- Always 00 00 00 00. 
          0x0C uint32 sequenceCount 
          0x10 + 16  Sequence File Name 
            RECORD DATA  + 32 * sequenceCount 
	            ?? string(\xFF\xFF) 16 Name 
	            ?? int32 4 FTree Owner (parent)
	            ?? int32 FTreeType (usually 0x03) 
	            ?? uint32 offset
	            ?? uint32 size
	
	    */

        public Dictionary<string, BARCLocation> Sequences;

        private BeBinaryReader barcread;

        private string readBARCString()
        {
            var ofs = barcread.BaseStream.Position;
            byte nextbyte;
            byte[] name = new byte[32];

            int count = 0;
            while ( (nextbyte = barcread.ReadByte()) != 0xFF  & nextbyte!=0x00 & nextbyte!=0x2E)
            {
                // http://xayr.ga/share/08-2018/devenv_2018-08-28_19-25-1449828102-6af8-4f41-9714-3332a90194d3.png
                name[count] = nextbyte;
                count++;
            }
            barcread.BaseStream.Seek(ofs + 16, SeekOrigin.Begin);
            return Encoding.ASCII.GetString(name, 0, count);
        }

        private string readBARCStringWE()
        {
            var ofs = barcread.BaseStream.Position;
            byte nextbyte;
            byte[] name = new byte[32];

            int count = 0;
            while ((nextbyte = barcread.ReadByte()) != 0xFF & nextbyte != 0x00)
            {
                name[count] = nextbyte;
                count++;
            }
            barcread.BaseStream.Seek(ofs + 16, SeekOrigin.Begin);
            return Encoding.ASCII.GetString(name, 0, count);
        }

        public BARCTree(byte[] chunk)
        {
            
            barcread = new BeBinaryReader(new MemoryStream(chunk));
            var bhead = barcread.ReadUInt64();
            if (bhead != 0x2D2D2D2D42415243)
            {
                Console.WriteLine("!!!! BARC header didn't match! Are you BARCing up the wrong tree? 0x{0:X}",bhead);
                return; 

            }

            barcread.ReadInt32(); // skip 
            var count = barcread.ReadInt32();
           
            Sequences = new Dictionary<string, BARCLocation>(count);
            var ARCFile  = readBARCStringWE();
            var vfile = File.Open(ARCFile, FileMode.Open, FileAccess.Read);
            for (int i=0; i < count; i++)
            {
                if (! Directory.Exists("./seqs_out"))
                {
                    Directory.CreateDirectory("./seqs_out");
                }
                var seqname = readBARCString() + ".bms";
                // Console.WriteLine("Sequence {1}/{0}", seqname,ARCFile);
                barcread.ReadUInt32();
                barcread.ReadUInt32(); // Idk, but im sure as hell not using these for this app. 
                var vx = new BARCLocation
                {
                    file = ARCFile,
                    index = (uint)i,
                    offset = barcread.ReadUInt32(),
                    size = barcread.ReadUInt32(),

                };



                Sequences[seqname] = vx; 
             
                byte[] john = new byte[vx.size];

                vfile.Seek(vx.offset, 0);
                vfile.Read(john, 0, (int)vx.size);

                if (!File.Exists("./seqs_out/" + seqname))
                {
                    File.WriteAllBytes("./seqs_out/" + seqname, john);
                }
               
            }



        }
	
	
    }
}
 