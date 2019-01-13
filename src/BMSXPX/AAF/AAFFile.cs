using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Be.IO;
using BMSXPX.AAF.Types;


namespace BMSXPX.AAF
{   
    // AAF Files are Big Endian clusters of data for the JAudio engine. 
    // They don't have an identifying header, but you can usually tell if something is yucky by checking if you've read past EOF. 
    // Each "header" is (usually) 16 bytes
    // <int32 ID> <int32 offset> <int32 size> <int32 type...> 
    // I don't know what the last one does, might be some sort of flag container of some sort, it's required to keep allignment
    // The last entry in the AAF header will always be 
    // id = 0, ofs = 0, size = 0, type = 0 
    // In a pinch, stop reading once you read a 00 type. it marks the end of the data. 

    class AAFFile
    {
        public AAFChunk[] Chunks;
        public Dictionary<string, BARCLocation> Sequences;
        private byte[] aafdata;
        private BeBinaryReader aafRead;
        public uint checksum;
        private Stack<AAFChunk> AAFTemp; 
        public Dictionary<int,WaveSystem> WaveSystems;
        public Dictionary<int, InstrumentBank> InstrumentBanks;

        private string convertChunkName(uint id)
        {
            switch (id)
            {
                case 0:
                    return "HEADER";
                case 4:
                    return "SEQBARK";
                case 2:
                    return "IBNK";
                case 3:
                    return "WAVESYSTEM";
                default:
                    return "UNKNOWN";
            }
        }
        public AAFFile(string filename)
        {

            AAFTemp = new Stack<AAFChunk>(64);  // We really shouldn't be seeing over 64 chunks.  Props to you if you prove me wrong. 
            aafdata = File.ReadAllBytes(filename);  // We're just going to load a whole copy into memory because we're lame -- having this buffer in memory makes it easy to pass as a ref to stream readers later. 
           
            aafRead = new BeBinaryReader(new MemoryStream(aafdata));
            WaveSystems = new Dictionary<int, WaveSystem>();
            InstrumentBanks = new Dictionary<int, InstrumentBank>();
          
            while (true) 
            {
              
                var rer = aafRead.ReadUInt32();


                Console.WriteLine("Parse ChunkID 0x{0:X} at 0x{1:X} ", rer, aafRead.BaseStream.Position);
                AAFChunk newchunk;

                switch (rer)
                {

                    case 1:
                    case 5:
                    case 4:
                    case 6:
                    case 7:
                        newchunk = new AAFChunk
                        {
                            id = rer, // + 4
                            offset = aafRead.ReadUInt32(), // + 4
                            size = aafRead.ReadUInt32(), // + 4
                            type = aafRead.ReadUInt32(), // + 4
                            rootchk = checksum

                        };
                        AAFTemp.Push(newchunk);
          

                        break;
                    case 2: // for some reason IBNK's are only 12 bytes instead of 16. 
                    case 3:
                        while (true)
                        {
                           

                            var ofs = aafRead.ReadUInt32();


                            if (ofs == 0)
                            {
                                break;
                            }
                           // Console.WriteLine("\t wsys flag at 0x{0:X}", aafRead.BaseStream.Position);
                            newchunk = new AAFChunk
                            {
                                id = rer, // + 4
                                offset = ofs, // + 4
                                size = aafRead.ReadUInt32(), // + 4
                                type = aafRead.ReadUInt32(), // + 4
                                subchunk = true,
                                rootchk = checksum

                            };
                            AAFTemp.Push(newchunk);
                       
                        }

                        break;
                    default:
                        Console.WriteLine("Unknown nonzero chunkid 0x{0:X} at 0x{1:X} ", rer, aafRead.BaseStream.Position);
                        break;
                                           

                }
           
             
                if (rer==0)
                {
                    Console.Write("Next chunk reads 0 at 0x{0:X}", aafRead.BaseStream.Position);
                    if (aafRead.ReadUInt32() == 0)
                    {
                        Console.Write(" ... And the offset data too, dropping loop ", aafRead.BaseStream.Position);
                        Console.WriteLine();
                        break;

                    }
                    Console.WriteLine();
                    continue;
                    
                }

        
            }

            Chunks = new AAFChunk[AAFTemp.Count]; // 

            for (int i= AAFTemp.Count - 1; i!=0; i--) // Reverse for loop, we're reading from the top of the stack down. 
            {
                Chunks[i] = AAFTemp.Pop(); 
            }

            ////// Debugging.

            for (int i=0; i < Chunks.Length ; i++)
            {
                var data = Chunks[i];
                var cn = convertChunkName(data.id); 
                if (data.subchunk == false)
                {
                    Console.WriteLine("Chunk {0} at 0x{1:X} size 0x{2:X} of type {3:X} \t {4}", data.id, data.offset, data.size, data.type,cn);
                }
                else
                {
                    Console.WriteLine("\t SubChunk {0} at 0x{1:X} size 0x{2:X} of type {3:X} \t {4}", data.id, data.offset, data.size, data.type,cn);
                }
            }

            long anchor = 0; 

            for (int i = 0; i < Chunks.Length; i++)
            {
                var data = Chunks[i];
                var evid = data.id; 
                switch(evid)
                {
                    case 4:  // BARC Tree
                        anchor = aafRead.BaseStream.Position; 

                        aafRead.BaseStream.Seek(data.offset,0);

                        var ParseBARC = new BARCTree(aafRead.ReadBytes((int)data.size));

                        break;
                    case 2:  // IBNK Only
                        var ParseINST = new InstrumentBank(ref aafRead, ref data);
                        InstrumentBanks[(int)ParseINST.id] = ParseINST;
                        break;
                    case 3: // WSYS
                        var ParseWSYS = new WaveSystem(ref aafRead, ref data);
                        WaveSystems[(int)ParseWSYS.wsysid] = ParseWSYS;
                        break;
                }
            }


        }
    }
}
