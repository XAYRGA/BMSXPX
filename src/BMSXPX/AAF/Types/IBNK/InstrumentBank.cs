using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Be.IO;
using System.IO;

namespace BMSXPX.AAF.Types
{



    public class Instrument
    {
        public float Volume;
        public float Pitch;
        public Stack<InstrumentVelocityRegion> VelRegions;
        public InstrumentVelocityRegion[][] Keys;

        public bool percussion; 
        public Instrument()
        {
            VelRegions = new Stack<InstrumentVelocityRegion>();
            Keys = new InstrumentVelocityRegion[0xFF][];
         


        }
    }

    public class InstrumentVelocityRegion
    {
        public float Volume;
        public float Pitch;
        public uint wave;
        public uint wsysid;
        public uint velocity;

    }

    public class InstrumentOscillator
    {

    }




    public class InstrumentBank
    {

        private const uint INST = 0x494E5354;
        private const uint PERC = 0x50455243;
        private const uint PER2 = 0x50455232;

        public uint size;
        public uint id;




        public Instrument[] Instruments;




        public InstrumentBank(ref BeBinaryReader aafRead, ref AAFChunk data)
        {

            Instruments = new Instrument[0xF0]; // We have room for 0xF0 instruments. 

            var evid = data.id; // Just the ID of the chunk carried over from the other data.

            aafRead.BaseStream.Seek(data.offset, SeekOrigin.Begin); // Seek to the offset of the current chunk

            var anchor = aafRead.BaseStream.Position; // Set an anchor point

            if (aafRead.ReadUInt32() != 0x49424e4b) // Check if it equals BANK
            {
                aafRead.BaseStream.Seek(-4, SeekOrigin.Current); // If it doesnt, seek back 4 
                Console.WriteLine("0x{0:X} Bad IBNK, expected 0x49424e4b got 0x{1:X} ID {2}", data.offset, aafRead.ReadUInt32(), evid); // Error. 
                return;
            }



            size = aafRead.ReadUInt32(); // Otherwise.... Read our size
            id = aafRead.ReadUInt32(); // This is the virtual ID of the instrument 

            Console.WriteLine("0x{0:X} IBNK sizeof(0x{1:X}) ID {2}", data.offset, size, id);

            aafRead.BaseStream.Seek(0x14, SeekOrigin.Current); // Always 0? Idk. 

            if (aafRead.ReadUInt32() != 0x42414E4B) // Every BANK should have an IBNK in it. 
            {
                aafRead.BaseStream.Seek(-4, SeekOrigin.Current);
                Console.WriteLine("\t 0x{0:X} Bad IBNK (BANK NOT FOUND), expected 0x42414E4B got 0x{1:X} ID {2}", data.offset, aafRead.ReadUInt32(), evid);
                return;
            }

            var instoffsets = new uint[0xF0]; // Load instrument offsets
            {
                for (int bkl = 0; bkl < 0xF0; bkl++) // Always 0xF0 offsets for 0xF0 instruments.
                {
                    var offs = aafRead.ReadUInt32(); // Its just a stack of int32's

                    if (offs > 0) // Unassigned ones will be 0.
                    {
                        instoffsets[bkl] = offs;
                        //Console.WriteLine("\t INST 0x{0:X} at 0x{1:X}", b, offs + anchor);
                    }

                }
            }

            int b = 0;
            for (int bv = 0; bv < 0xF0; bv++) // Always 0xF0 offsets for 0xF0 instruments.
            {   
                uint coffs = instoffsets[bv];  // Current offset. 
                b = bv;
                if (coffs != 0)
                {
                   
                    var absoff = coffs + anchor; // absolute offset 

                    aafRead.BaseStream.Position = absoff; // Seek to here + anchor pos. 

                    var cinst = aafRead.ReadUInt32(); // Get current identity
                    switch (cinst)
                    {
                        case INST: // if it equals INST
                          

                            {
                                bool debug = false;
                                if (b == 43 & id==4)
                                {
                                    // debug = true;  
                                }
                                var NewINST = new Instrument();

                                aafRead.BaseStream.Seek(4, SeekOrigin.Current); // always 0x00
                                var pitch = aafRead.ReadSingle(); // Base Pitch
                                var vol = aafRead.ReadSingle(); // Base Volume

                                NewINST.Pitch = pitch;
                                NewINST.Volume = vol;



                                uint[] OSCOffsets = new uint[2]; // Always 2.
                                OSCOffsets[0] = aafRead.ReadUInt32(); // Offset of first oscillator table
                                OSCOffsets[1] = aafRead.ReadUInt32(); // offset of second oscillator table

                                uint[] EFFOffsets = new uint[2]; // Always 2.
                                EFFOffsets[0] = aafRead.ReadUInt32(); // offset of first effect
                                EFFOffsets[1] = aafRead.ReadUInt32(); // offset of second effect.

                                uint[] SENOffsets = new uint[2]; // Always 2.
                                SENOffsets[0] = aafRead.ReadUInt32(); // offset of first sensor effect
                                SENOffsets[1] = aafRead.ReadUInt32(); // offset of second sensor effect

                                var keyCounts = aafRead.ReadUInt32();

                                uint[] KEYOffsets = new uint[keyCounts];

                                if (debug)
                                {
                                    Console.WriteLine("KeyCounts {0}", keyCounts);
                                    Console.ReadLine();
                                }
                                for (int i = 0; i < keyCounts; i++)
                                {
                                    KEYOffsets[i] = aafRead.ReadUInt32();
                                }
                                


                                var KeyLow = 0; // This method stolen from Jasper. 
                                var KeyHigh = 0;

                                for (int i = 0; i < keyCounts; i++)
                                {



                                    var coffsx = KEYOffsets[i] + anchor;
                                    aafRead.BaseStream.Position = coffsx;
                                    byte key = aafRead.ReadByte();
                                    if (debug)
                                    {
                                        Console.WriteLine("KL: {0}", KeyLow);
                                      
                                    }
                                    KeyHigh = key;
                                    if (debug)
                                    {
                                        Console.WriteLine("KH: {0}", KeyHigh);
                                        Console.ReadLine();
                                    }


                                    if (key > 127)
                                    {
                                        Console.WriteLine("0x{0:X} Bad key 0x{1:X}", coffs, key);
                                        continue;
                                    }



                                    aafRead.BaseStream.Seek(3, SeekOrigin.Current);
                                    var velRegionCounts = aafRead.ReadUInt32();
                                    //  Console.WriteLine(velRegionCounts);

                                    var velRegionsOffsets = new uint[velRegionCounts];
                                    // Console.WriteLine("\t {0} Velociy Regions for this key. ", velRegionCounts);
                                    //Console.ReadLine();
                                    for (int x = 0; x < velRegionCounts; x++)
                                    {
                                        velRegionsOffsets[x] = aafRead.ReadUInt32();

                                    }
                                    if (velRegionCounts > 1)
                                    {
                                        Console.ForegroundColor = ConsoleColor.Yellow;
                                    }

                                    if (debug)
                                    {
                                        Console.WriteLine("VR Counts: {0}", velRegionCounts);
                                        Console.ReadLine();
                                    }

                                    var InstKeys = new InstrumentVelocityRegion[0x81];
                                    var VelLow = 0;
                                    var VelHigh = 0;
                                    var vt_total = 0;
                                    InstrumentVelocityRegion LastRegion; 
                                    for (int x = 0; x < velRegionCounts; x++)
                                    {
                                        if (debug)
                                        {
                                            Console.WriteLine("\tVL: {0}", VelLow);
                                       
                                        }
                                        // Console.WriteLine(velRegionCounts);
                                        var voffsv = velRegionsOffsets[x] + anchor;

                                        aafRead.BaseStream.Position = voffsv;

                                        var velocity = aafRead.ReadByte();
                                        VelHigh = velocity;

                                        var NewRegion = new InstrumentVelocityRegion();
                                        LastRegion = NewRegion;

                                        aafRead.BaseStream.Seek(3, SeekOrigin.Current);

                                        var wsysid = aafRead.ReadUInt16();
                                        // Console.WriteLine("INST req wsid {0}", wsysid);
                                        // Console.ReadKey();

                                        if (id == 4)
                                        {
                                            // Console.WriteLine("INST req wsid {0}", wsysid);
                                            //  Console.ReadKey();
                                        }

                                        var waveid = aafRead.ReadInt16();

                                        var volume = aafRead.ReadSingle();

                                        var pitch2 = aafRead.ReadSingle();

                                        NewRegion.Volume = volume;
                                        NewRegion.Pitch = pitch2;
                                        NewRegion.wave = (uint)waveid;
                                        NewRegion.wsysid = wsysid;
                                        NewRegion.velocity = velocity;
                                       
                                        for (int idx = 0; idx < (VelHigh - VelLow); idx++)
                                        {
                                            vt_total++;
                                           // Console.WriteLine(VelLow + idx);
                                            InstKeys[(VelLow + idx) ] = NewRegion;
                                        }

                                        InstKeys[127] = LastRegion; // Hax. 
                                        if (debug)
                                        {
                                            Console.WriteLine("\tVH: {0}", VelHigh);
                                            Console.ReadLine();
                                        }


                                        //Console.WriteLine("0x{6:X}, idata 0x{3:X}\t Key {5} Wave {0} vol {1} pit {2} vel {4} group {7}", waveid, volume, pitch2,voffsv,velocity,key, instoffsets[b] + anchor, wsysid);
                                        //Console.WriteLine(waveid);
                                        // Console.WriteLine(VelHigh);
                                        VelLow = VelHigh;

                                    }

                                    if (vt_total < 127)
                                    {
                                       
                                    }
                                    if (velRegionCounts > 1)
                                    {
                                        Console.ForegroundColor = ConsoleColor.Gray;
                                    }

                                    for (int idx = 0; idx < (KeyHigh - KeyLow); idx++)
                                    {

                                        NewINST.Keys[(KeyLow + idx) ] = InstKeys;
                                    }

                                    KeyLow = KeyHigh;

                                }
                                //Console.WriteLine("Add Instrument {0} in bank {1}", b,id);

                                for (int ib = 0; ib < NewINST.Keys.Length; ib++)
                                {

                                }
                                Instruments[b] = NewINST;
                                /*
                                // I'll do this later.  i just want to get the keys / waveids for now
                                for (int i = 0; i < 2; i++)
                                {
                                    var offx = OSCOffsets[i];
                                    if (offx > 0)
                                    {
                                        aafRead.BaseStream.Position = anchor + offx;
                                        var mode = aafRead.ReadSByte();
                                        aafRead.BaseStream.Seek(3, SeekOrigin.Current);

                                        var rate = aafRead.ReadSingle();

                                        var leadin_taboffs = aafRead.ReadUInt32();
                                        var leadout_taboffs = aafRead.ReadUInt32();

                                        var width = aafRead.ReadSingle();
                                        var base_e = aafRead.ReadSingle(); 

                                        if (leadin_taboffs!=0)
                                        {

                                        }
                                    }

                                }



                                TODO: LOAD EFFECTS HERE

                                TODO: LOAD SENSOR EFFECFTS 


                                */
                            }
                            break;
                        case PERC:

                            break;
                        case PER2:
                            {

                                // Almost identical to INST but it doesn't have key counts????
                                // Seems like the assigned key is just the index in the table, interesting. 
                                InstrumentVelocityRegion LastReg = new InstrumentVelocityRegion();
                                Console.ForegroundColor = ConsoleColor.Red;
                                var NewPERC = new Instrument();
                                NewPERC.percussion = true; 
                                 
                                aafRead.BaseStream.Seek(0x84, SeekOrigin.Current); // according to jasper there's never anything before 0x88. -4 because we've already read PER2. 
                                for (int i = 0; i < 100; i++)
                                {

                                    var krofs = aafRead.ReadUInt32(); // relative jump position of key region
                                  
                                    var jmpret_ofs = aafRead.BaseStream.Position;  // store return position for later, we've already read the int so we've advanced 4 bytes. 
                                    if (krofs == 0)
                                    {
                                        aafRead.BaseStream.Position = jmpret_ofs; // return to position 
                                        continue; // is not a good one. skip it.
                                    }
                             

                                    aafRead.BaseStream.Seek(krofs + anchor, 0); // seek to region table. 
                                 
                                    // !!!! LAYOUT OF THIS SECTION IS ACTUALLY FUCKED
                                    NewPERC.Pitch = aafRead.ReadSingle(); // 0x04 
                                    NewPERC.Volume = aafRead.ReadSingle();  // Identical to inst ..... and to think i removed this :V

                                    aafRead.BaseStream.Seek(8, SeekOrigin.Current);
                                    // now at 0x10 
                                    var VelRCount = aafRead.ReadUInt32(); // how many regions there are
                                    var CVR_OFFSET = aafRead.ReadUInt32();  // offset to velocity region table. 

                                    var InstKeys = new InstrumentVelocityRegion[0x81];
                                    var VelHigh = 0;
                                    var VelLow = 0;
                                  
                                    for (int idx = 0; idx < VelRCount; idx++)
                                    {



                                        aafRead.BaseStream.Position = CVR_OFFSET + anchor;

                                        var velocity = aafRead.ReadByte();
                                        VelHigh = velocity;

                                        var NewRegion = new InstrumentVelocityRegion();


                                        aafRead.BaseStream.Seek(3, SeekOrigin.Current);

                                        var wsysid = aafRead.ReadUInt16();
                                       


                                        var waveid = aafRead.ReadInt16();

                                        var volume = aafRead.ReadSingle();

                                        var pitch2 = aafRead.ReadSingle();

                                        NewRegion.Volume = volume;
                                        NewRegion.Pitch = pitch2;
                                        NewRegion.wave = (uint)waveid;
                                        
                                        NewRegion.wsysid = wsysid;
                                        NewRegion.velocity = velocity;
                                        /// ha, vraptor. for velociraptor. . . . I'll leave now. 
                                        /// 
                                        //Console.WriteLine("KID: {3} WID: {0} VRAPTOR: {1}?=127 WSID: {2}", waveid,velocity,wsysid,i);


                                      
                                       
                                        for (int idb = 0; idb < (VelHigh - VelLow); idb++)
                                        {
                                            InstKeys[(VelLow + idb) ] = NewRegion; // Fun, apparerntly they're 1 based. 
                                        }

                                        LastReg = NewRegion;
                                        //Console.WriteLine("0x{6:X}, idata 0x{3:X}\t Key {5} Wave {0} vol {1} pit {2} vel {4} group {7}", waveid, volume, pitch2,voffsv,velocity,key, instoffsets[b] + anchor, wsysid);
                                        //Console.WriteLine(waveid);

                                        VelLow = VelHigh;



                                    }


                                    InstKeys[127] = LastReg;
                                    NewPERC.Keys[i] = InstKeys;
                                   
                                    aafRead.BaseStream.Position = jmpret_ofs; // return to position 



                                }
                             
                                Instruments[b] = NewPERC;
                            }




                            break;
                        default:
                            Console.WriteLine("Unknwon INST type {0:X} @ {1:X}", cinst, absoff);
                            break;



                    }

                }

            }





        }
    }
}
