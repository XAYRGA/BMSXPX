using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Be.IO;
using System.IO;
/*A MASSIVE thanks to Jasper (aka magicus) for their patience, and helping me with this ADPCM decoder as well as other stuff. 
 * https://github.com/magcius
 */

namespace BMSXPX.AAF.Types
{
    public class WaveSystem
    {

        public uint wsysid; 

        public uint GroupCount;
        public uint SceneCount;

        public WSYSGroup[] WaveGroups;
        public Dictionary<int, WSYSWave> SampleMap; 



        private BeBinaryReader aafRead;
        private AAFChunk data;
        private uint anchor;
        private uint size;


        private uint[] wgrp_offsets;
        private uint[] wbct_offsets;



        private const int WSYS = 0x57535953;
        private const int SCNE = 0x53434E45;
        private const int C_DF = 0x432D4446;


        ushort[] afccoef = new ushort[16]
            {
            0,
            0x0800,
            0,
            0x0400,
            0x1000,
            0x0e00,
            0x0c00,
            0x1200,
            0x1068,
            0x12c0,
            0x1400,
            0x0800,
            0x0400,
            0xfc00,
            0xfc00,
            0xf800,
                //? Array error
            };

        ushort[] afccoef2 = new ushort[16]
        {
            0,
            0,
            0x0800,
            0x0400,
            0xf800,
            0xfa00,
            0xfc00,
            0xf600,
            0xf738,
            0xf704,
            0xf400,
            0xf800,
            0xfc00,
            0x0400,
            0,
            0,
        };





        public WaveSystem(ref BeBinaryReader inread, ref AAFChunk blockdata)
        {
            var buff = new byte[0xF];
            aafRead = inread; // import readers. 
            data = blockdata; // Import readers 

            SampleMap = new Dictionary<int, WSYSWave>();

            anchor = data.offset;  // set anchor

            Console.WriteLine("Wave System at 0x{0:X6}", anchor);

            aafRead.BaseStream.Seek(anchor, 0); // seek to address pos 


            var hdr = aafRead.ReadUInt32();
            if (hdr != WSYS)
            {
                Console.WriteLine("Error: Section block is ID Type 3 (0x57535953) but is actually 0x{0:X}", hdr);
                return;
            }



            var size = aafRead.ReadUInt32(); // Section size. 
            wsysid = aafRead.ReadUInt32(); // 4 byte wsys identifier 1/3/2019 
            aafRead.ReadUInt32(); // 4 bytes, not used. .. I think


            var winfofs = aafRead.ReadUInt32();
            var wbctofs = aafRead.ReadUInt32();

            loadWINF(winfofs);
            loadWBCT(wbctofs);

            if (SceneCount != GroupCount)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("ERROR: Wave group count and scene count do not match. {0:X} | {1:X}\nloading this wavesystem halted.", GroupCount, SceneCount);
                return;
            }

            WaveGroups = new WSYSGroup[GroupCount];

            for (int wg = 0; wg < GroupCount; wg++)
            {
                aafRead.BaseStream.Position = wgrp_offsets[wg] + anchor;
                var arc_name = readArchiveName();  // + 112 & 0x70
                Directory.CreateDirectory("./WAVESYSTEM/AW_" + arc_name);
                var fobj = File.OpenRead("./Banks/" + arc_name);
                var fobj_reader = new BeBinaryReader(fobj);

                var waveInfoCount = aafRead.ReadUInt32();
                var info_offsets = new uint[waveInfoCount];

                for (int i = 0; i < waveInfoCount; i++)
                {
                    info_offsets[i] = aafRead.ReadUInt32();
                }

                //  Console.WriteLine("{0} - {1}", arc_name, waveInfoCount);



                aafRead.BaseStream.Position = wbct_offsets[wg] + anchor;
                var hdr2 = aafRead.ReadUInt32();

                if (hdr2 != SCNE)
                {
                    Console.WriteLine("[0x{3:X6}] MEMORY ERROR: Expected 0x{1:X} but got 0x{0:X}", hdr2, SCNE, aafRead.BaseStream.Position);
                }


                aafRead.ReadUInt64(); // Skip 8 bytes, basically. 

                aafRead.BaseStream.Position = anchor + aafRead.ReadUInt32(); // Moves to C-DF

                hdr2 = aafRead.ReadUInt32();

                if (hdr2 != C_DF)
                {
                    Console.WriteLine("[0x{3:X6}] MEMORY ERROR: Expected 0x{1:X} but got 0x{0:X}", hdr2, C_DF, aafRead.BaseStream.Position);
                }

                var idcount = aafRead.ReadUInt32();

                var idofs = new uint[idcount];


                for (int i = 0; i < idcount; i++)
                {
                    //Console.WriteLine("0x{0:X},",aafRead.BaseStream.Position)
                    idofs[i] = aafRead.ReadUInt32();
                }

                var newGroup = new WSYSGroup(waveInfoCount);

                for (int widx = 0; widx < waveInfoCount; widx++)
                {
                    var newWave = new WSYSWave();

                    aafRead.BaseStream.Position = idofs[widx] + anchor;

                    //Console.WriteLine("0x{0:X}", aafRead.BaseStream.Position);
                    var awid = aafRead.ReadInt16();
                    var wtf = aafRead.ReadInt16();
                    newWave.id = ((uint)wtf);

                   //Console.WriteLine("WID 0x{0:X} inside {1} at 0x{2:x}", wtf,vidx,aafRead.BaseStream.Position);
                   //Console.ReadKey();
                    aafRead.BaseStream.Position = info_offsets[widx] + anchor;

                    aafRead.ReadByte(); // skip 
                    newWave.format = aafRead.ReadByte();
                    newWave.key = aafRead.ReadByte();
                    aafRead.ReadByte(); // skip 

                    buff = aafRead.ReadBytes(4);
                    newWave.sampleRate = ((buff[1] << 8) | buff[2]) / 2;
                    if (newWave.format == 5)
                    {
                        newWave.sampleRate = 32000; /// ????
                    }

                    newWave.w_start = aafRead.ReadUInt32();
                    newWave.w_size = aafRead.ReadUInt32();
                    newWave.loop = aafRead.ReadUInt32()==UInt32.MaxValue;
                    newWave.loop_start = aafRead.ReadUInt32() / 8 * 16 ;
                    newWave.loop_end = aafRead.ReadUInt32() ;
                    newWave.sampleCount = newWave.w_size / 9 * 16;

                    var name = string.Format("0x{0:X}.wav", newWave.id);
                    var name2 = string.Format("0x{0:X}.par", newWave.id);
                    newWave.pcmpath = "./WAVESYSTEM/AW_" + arc_name + "/" + name;
                    WSYSWave Check;
                    if (SampleMap.TryGetValue(wtf, out Check))
                    {
                        Console.WriteLine("[!] Duplicate WSYSWave ID WSYS: {0} ID:{1} ", wsysid, wtf);
                    }
                    SampleMap[wtf] = newWave;
                    if (File.Exists("./WAVESYSTEM/AW_" + arc_name + "/" + name))
                    {
                        continue;  // Reduces load time, if the sample is already there or has been replaced then ignore it.
                    }
                    var fobjout = File.Open("./WAVESYSTEM/AW_" + arc_name + "/" + name, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                    var fobj_writer = new BinaryWriter(fobjout);
                    


                    File.WriteAllText("./WAVESYSTEM/AW_" + arc_name + "/" + name2, "" + newWave.loop + "\n" + newWave.loop_start + "\n" + newWave.loop_end);
                    
                

                    /* Below is SHAMELESSLY ripped from WWDumpSND */
                    unchecked
                    {

                        /******* DECODE AFC TO PCM *********/

                        int hi0 = 0;
                        int hi1 = 0;

                        int framesz = 9;
                        int osz = (int)newWave.w_size / framesz * 16 * 2;
                        int oszt = osz + 8;
                        int size_rem;
                        short[] wavout;
                        byte[] wavin;

                        /////****** WAV BUFFER ******/////

                        byte[] wavhead = new byte[44] {
                        0x52, 0x49, 0x46, 0x46, 0x00, 0x00, 0x00, 0x00,  0x57, 0x41, 0x56, 0x45, 0x66, 0x6D, 0x74, 0x20,
                        0x10, 0x00, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00,  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                        0x02, 0x00, 0x10, 0x00, 0x64, 0x61, 0x74, 0x61,  0x00, 0x00, 0x00, 0x00
                    };

                        

                        for (int i = 0; i < wavhead.Length; i++)
                        {
                            fobj_writer.Write(wavhead[i]);

                        }




                  





                        fobj_writer.BaseStream.Position = 4;


                        fobj_writer.Write(oszt);



                        fobj_writer.BaseStream.Position = 24;

                        fobj_writer.Write((int)newWave.sampleRate);

                        fobj_writer.Write((int)newWave.sampleRate * 2);


                        fobj_writer.BaseStream.Position = 40;

                        fobj_writer.Write((int)osz);




                        

                        fobj_reader.BaseStream.Position = newWave.w_start;

                        byte cbyte = 0;
                        sbyte[] nibbles;

                        for (size_rem = (int)newWave.w_size; size_rem >= framesz; size_rem -= framesz)
                        {

                            wavin = fobj_reader.ReadBytes(framesz);
                            wavout = new short[16];
                            var wavreader = new BeBinaryReader(new MemoryStream(wavin));

                            /********* AFC Decoder buffer **********/

                            cbyte = wavreader.ReadByte(); // READ BYTE 0, ADVANCE TO 1


                            int scale = ( 1 << ( cbyte >> 4)) ;


                            //Console.WriteLine("Delta {0} - {1} FSZ {2}", scale, (short)((cbyte) >> 4),framesz);

                            short index = (short)(cbyte & 0xF);
                            //Console.WriteLine("Index {0}", index);

                            nibbles = new sbyte[16];

                           

                       
                            if (newWave.format == 0)
                            {
                                for (int i = 0; i < 16; i += 2)
                                {
                                    cbyte = wavreader.ReadByte(); // src ++ 
                                    var bse = (sbyte)(cbyte);
                                    nibbles[i + 0] = (sbyte) (bse >> 4);
                                    nibbles[i + 1] = (sbyte)(cbyte & 15);

                                   // Console.WriteLine("NIBBLES: {0} {1}", nibbles[i + 0], nibbles[i  + 1]);

                                 
                                }

                                for (int i = 0; i < 16; i++)
                                {
                                    if (nibbles[i] >= 8)
                                    {
                                        nibbles[i] = (sbyte)(nibbles[i] - 16);

                                    }
                                  
                                }
                            
                            }
                            else
                            {
                                /*
                                for (int i = 0; i < 16; i += 4)
                                {
                                    nibbles[i + 0] = (short)((cbyte >> 6) & 0x03);

                                    nibbles[i + 1] = (short)((cbyte >> 4) & 0x03);
                                    nibbles[i + 2] = (short)((cbyte >> 2) & 0x03);
                                    nibbles[i + 3] = (short)((cbyte >> 0) & 0x03);


                                    cbyte = wavreader.ReadByte(); // src ++ 
                                }

                                for (int i = 0; i < 16; i++)
                                {
                                    if (nibbles[i] >= 2)
                                    {

                                        nibbles[i] = (short)(nibbles[i] - 4);
                                        nibbles[i] = (short)(nibbles[i] << 13);


                                    }

                                     

                                }



                            }

                           
                                */

                            }


                            for (int i = 0; i < 16; i++)
                            {
                                //Console.WriteLine(nibbles[i]);

                                var superscale = ((scale * nibbles[i]) << 11);
                                var coef1_addi = (int)hi0 * (short)afccoef[index];
                                var coef2_addi = (int)hi1 * (short)afccoef2[index];
                                var final0 = superscale + coef1_addi + coef2_addi;
                                var final1 = final0 >> 11;


                              


                                int sample = final1;



                                
                                //Console.ReadLine();
                                // CLAMP 16 BIT PCM
                                //Console.WriteLine("XATA scc {0} c1a {1} c2a {2} f0 {3} f1 {4}", superscale,coef1_addi,coef2_addi,final0,final1);
                                //Console.WriteLine("Data scl {0} nibi {1} hi0 {2} hi1 {3} c1 {4:X6} c2 {5:X6}", scale, nibbles[i], hi0,hi1,afccoef[index],afccoef2[index]);
                                //Console.WriteLine("Sample {0}", final1);
                                //Console.ReadLine();

                                if (sample > 32767)
                                {
                                    sample = 32767;
                                }
                                if (sample < -32768)
                                {
                                    sample = -32768;
                                }


                                wavout[i] = (short)(sample);
                                hi1 = hi0;
                                hi0 = sample;
                            }



                            for (int i = 0; i < 16; i++)
                            {
                                fobj_writer.Write(wavout[i] );
                            }



                        }


                        // Console.WriteLine("osz {0} {1}", osz, fobj_writer.BaseStream.Length);
                        fobj_writer.Flush();
                        fobj_writer.Close();




                        /* Fuck it, will rely on WWDumpSND. i keep getting corrupted PCM data.
                                          * 
                                          * 
                                       //  Console.WriteLine(newWave.sampleCount);
                                        // Console.ReadLine();

                                         BeBinaryWriter wave_pcm = new BeBinaryWriter(new MemoryStream());
                                         adpcm_convert.DecodeStreamAdpcm(fobj_reader, wave_pcm,(int)newWave.sampleCount);
                                         var name = string.Format("0x{0:X}.pcm", newWave.id);
                                         var wave_size = wave_pcm.BaseStream.Length;

                                         var wave_out = new byte[wave_size];
                                         wave_pcm.Seek(0, 0);
                                         wave_pcm.BaseStream.Read(wave_out, 0, (int)wave_size);


                                         File.WriteAllBytes("./seqout/o_" + arc_name + "/" + name, wave_out);


                                         // Console.WriteLine("{0} {1} {2}", newWave.id, newWave.key, newWave.sampleRate);
                                         */


                        newGroup.waves[newWave.id] = newWave;

                    }

                }
                Console.WriteLine("{0} Wave ID's", idcount);

                WaveGroups[wg] = newGroup;

            }






        }



        private string readArchiveName()
        {
            var ofs = aafRead.BaseStream.Position;
            byte nextbyte;
            byte[] name = new byte[0x70];

            int count = 0;
            while ((nextbyte = aafRead.ReadByte()) != 0xFF & nextbyte != 0x00)
            {
                name[count] = nextbyte;
                count++;
            }
            aafRead.BaseStream.Seek(ofs + 0x70, SeekOrigin.Begin);
            return Encoding.ASCII.GetString(name, 0, count);
        }

        private void loadWINF(uint ataddress)
        {
            aafRead.BaseStream.Position = anchor + ataddress;
            aafRead.ReadUInt32(); // Alignment, should be WINF. 

            GroupCount = aafRead.ReadUInt32();

            wgrp_offsets = new uint[GroupCount];

            for (int wg = 0; wg < GroupCount; wg++)
            {
                wgrp_offsets[wg] = aafRead.ReadUInt32();
            }


        }

        private void loadWBCT(uint ataddress)
        {
            aafRead.BaseStream.Position = anchor + ataddress;
            aafRead.ReadUInt32(); // Alignment, should be WBCT. 
            aafRead.ReadUInt32(); // skip 4 bytes. 

            SceneCount = aafRead.ReadUInt32();

            wbct_offsets = new uint[SceneCount];

            for (int wg = 0; wg < SceneCount; wg++)
            {
                wbct_offsets[wg] = aafRead.ReadUInt32();

            }


        }

    }
}
