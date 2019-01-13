using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Be.IO;
using System.Threading;
using System.Diagnostics;
using BMSXPX.AAF;

namespace BMSXPX.BMS
{
    class BMSSequencer
    {
        static BMSSubroutine MainRoutine;
        static BMSSubroutine[] SongRoutines;
        static BMSSubroutineInfo SongInfo;
        static BMSAudioEngine AudioEngine; 
        static int bpm;
        static int ppqn;

        static float tick_len;

        static Thread PlaybackThread;

        private static BMSSubroutine playing_routine;
        static long totalticks = 0; 

        static ConsoleColor[] channelcolors = new ConsoleColor[]
        {
            ConsoleColor.Yellow,

            ConsoleColor.Red,
            ConsoleColor.Green,
            ConsoleColor.Red,
            ConsoleColor.Green,
            ConsoleColor.Red,
            ConsoleColor.Green,
            ConsoleColor.DarkCyan,
            ConsoleColor.Magenta,
            ConsoleColor.DarkCyan,
            ConsoleColor.Magenta,
            ConsoleColor.DarkCyan,
            ConsoleColor.Magenta,
            ConsoleColor.DarkYellow,
            ConsoleColor.DarkMagenta,
            ConsoleColor.DarkYellow,
            ConsoleColor.DarkMagenta,
            ConsoleColor.DarkYellow,
            ConsoleColor.DarkMagenta,

        };

        public BMSSequencer(BMSSubroutineInfo MainInfo, BMSSubroutine main, BMSSubroutine[] Rots, AAFFile AAFData,BMSSubroutineInfo[] subroutineInfos)
        {
            AudioEngine = new BMSAudioEngine(AAFData);
            MainRoutine = main;
            SongRoutines = new BMSSubroutine[Rots.Length + 1];

            SongRoutines[0] = MainRoutine;
            for (int id = 0; id < MainInfo.usedInstCount; id++)
            {
                var instinfo = MainInfo.usedInstruments[id];
                AudioEngine.preloadInst(instinfo.bank, instinfo.inst);
            }


            for (int idx = 0; idx < subroutineInfos.Length; idx++)
            {
                var instinfo = subroutineInfos[idx];
                if (instinfo==null) { break; };
                for (int id = 0; id < instinfo.usedInstCount; id++)
                {
                    var instinfo2 = instinfo.usedInstruments[id];
                    AudioEngine.preloadInst(instinfo2.bank, instinfo2.inst);
                }
        
            }


            for (int i=0; i < Rots.Length; i++)
            {
                SongRoutines[1 + i] = Rots[i];
                AudioEngine.AddChannel(i);
            }
           

            bpm = MainInfo.bpm;
            ppqn = MainInfo.ppqn;

            if (bpm==0)
            {
                bpm = 120; 

            }

            if (ppqn==0)
            {
                ppqn = 120;
            }

            tick_len = (float)(40000 / ((float)bpm + 0.01f)) / ((float)ppqn + 0.01f); // The +0.'s are so we never dividde by zero

            Console.WriteLine("Sequencer loaded with tick len {0}", tick_len);

            PlaybackThread = new Thread(new ThreadStart(playback_tick));

           // WaveStream mainOutputStream = new Mp3FileReader("test.mp3");
           // WaveChannel32 volumeStream = new WaveChannel32(mainOutputStream);

           // WaveOutEvent player = new WaveOutEvent();

            // player.Init(volumeStream);

            //; player.Play();

        }

        private static void update_ticklen()
        {
            tick_len = (40000 / ((float)bpm + 0.01f)) / (ppqn + 0.01f);
        }
        public void StartPlayback()
        {
            PlaybackThread.Start();
        }

        private static void NOP(double miliseconds)
        {
            var durationTicks = miliseconds * (Stopwatch.Frequency / 1000);
            var sw = Stopwatch.StartNew();

            while (sw.ElapsedTicks < durationTicks)
            {
               
            }
        }

        private static void skip(int bytes)
        {
            playing_routine.skip(bytes);
        }

        private static void playback_tick()
        {
            try {
                while (true)
                {
                    totalticks++;
                    for (int routine = 0; routine < SongRoutines.Length; routine++)
                    {
                        var CurrentRot = SongRoutines[routine];
                        if (CurrentRot != null)
                        {
                            Console.ForegroundColor = channelcolors[routine];
                            playing_routine = CurrentRot;
                            if (CurrentRot.Delay > 0 & !CurrentRot.stopped) { CurrentRot.Delay--; }
                           // Console.WriteLine("{0} -- Current Delay is {1}", routine,CurrentRot.Delay);
                            if (CurrentRot.Delay == 0 & !CurrentRot.stopped)
                            {
                                
                                var opcode = CurrentRot.getOpcode();
                                //Console.WriteLine("EXEC OPC {1}  - {0:X} ", opcode, routine);

                                ///// SPECIAL CASE OPCODES /////
                                                          
                                if (opcode < 0x80)
                                {
                                    var note = opcode; // IM A FUCKING MORON. I forgot that the fucking note value IS THE COMMAND ITSELF 
                                    var voice = CurrentRot.getOpcode(); // int 8 
                                    var vel = CurrentRot.getOpcode();

                                    //Console.WriteLine("[{3}]{0} note on {1} @ {2}", routine, note, vel,totalticks);
                                    AudioEngine.StartSound(routine, note,voice , vel);
                                }
                                else if (opcode == 0x80)
                                {
                                    var del = CurrentRot.getOpcode();
                                    CurrentRot.AddDelay(del);
                                   // Console.WriteLine("{0} Delay8 {1} ticks", routine, del);
                                }
                                else if (opcode < 0x88)
                                {
                                    var voice = opcode & 0x7F; // Channels have 8 voices, 0x80 - 0x87, i however don't want the last bit, so i only get 1-7.
                                    // That will be the voice ID. 
                                    //Console.WriteLine("{0} note off ",routine);
                                    AudioEngine.StopSound(routine,voice);

                                }
                                else if (opcode == 0x88)
                                {
                                    var del = CurrentRot.getOpword();
                                    CurrentRot.AddDelay(del);
                                   // Console.WriteLine("{0} Delay16 {1} ticks", routine, del);
                                }

                                else // NONSPECIAL OPCODES. 
                                {
                                    switch (opcode)
                                    {
                                        case 0xA0:
                                            skip(2);
                                            break;
                                        case 0x98: // BPRM u8?, u8?  MML_PERF_S8_NODUR = 0x98,  (u16?) 
                                            skip(2);
                                            break;
                                        case 0x9A: //     MML_PERF_S8_DUR_U8 = 0x9A, op u8 (event), pn u8 (pan), du u8 (duration) ( Pan Change )
                                            {       //event 0x3 = change panning 
                                                    //
                                                    /*/
                                                        MML_VOLUME = 0,
                                                        MML_PITCH = 1,
                                                        MML_REVERB = 2,
                                                        MML_PAN = 3,
                                                        */
                                                var type = CurrentRot.getOpcode();
                                                var val = CurrentRot.getOpcode();
                                                var dur = CurrentRot.getOpcode();

                                             //   skip(3);
                                            }
                                            break;
                                        case 0x9C: // VOLV op u8 (event),in u8 (intensity),du u8 (duration) ( Volume Event ) 
                                            //op 0x00 = Volume change
                                            //op 0x09 = Virbrato change 
                                            skip(3);
                                            break;
                                        case 0x9E:
                                            skip(4);
                                            break;
                                        case 0xA3:
                                            skip(2);
                                            break;
                                        case 0xA4:// ISVC op u8 (event), bnk u8 (bank / ins)  
                                            // op 0x20 = bank change 
                                            // op 0x21 = Inst change
                                            {
                                                var type = CurrentRot.getOpcode();
                                                var value = CurrentRot.getOpcode();
                                                if (type == 0x20)
                                                {
                                                    Console.WriteLine("BANK CHANGE");
                                                    AudioEngine.ChangeChannelBank(routine, value);

                                                }
                                                else if (type == 0x21)
                                                {
                                                    Console.WriteLine("PROGRAM CHANGE");
                                                    AudioEngine.ChangeChannelProgram(routine, value);
                                                }

                                            }
                                            break;
                                        case 0xA5:
                                            skip(2);
                                            break;
                                        case 0xA7:
                                            skip(2);
                                            break;
                                        case 0xA9:
                                            skip(4);
                                            break;
                                        case 0xAA:
                                            skip(4);
                                            break;
                                        case 0xAC:
                                            skip(3);
                                            break;
                                        case 0xAD:
                                            skip(3);
                                            break;
                                        case 0xAE: // STBR -- Set timebuffer

                                            break;
                                        case 0x8C: // TDIV -- Timebuffer / 2 

                                            break; 
                                        case 0xB1:
                                            int flag = CurrentRot.getOpcode();
                                            if (flag == 0x40) { skip(2); }
                                            if (flag == 0x80) { skip(4); }
                                            break;
                                        case 0xC1: // SUBR ptr u24, (Define subroutine) (Should never be called outside of main!) 
                                            
                                            break;
                                        case 0xB8: // unkop (2)
                                            skip(2);
                                            break;
                                        case 0xc2: // unkop
                                            skip(1);
                                            break;
                                        case 0xb4: // unkop (4)
                                            skip(4);
                                            break; 
                                        case 0xc6: // RETN <u8????>  -- Pop address from stack, jump.
                                            skip(1);
                                            break;
                                        case 0xc7:
                                            skip(4);
                                            break;
                                        case 0xc8: // JMP, I24 
                                            var mode = CurrentRot.getOpcode();
                                            var offset = CurrentRot.readUInt24BE();
                                            Console.WriteLine("===========================================");
                                            Console.WriteLine("{0} Jumps to 0x{1:X} on condition {2} ", routine, offset,mode);
                                            Console.WriteLine("===========================================");
                                            CurrentRot.JumpTo(offset);
                                            break;
                                        case 0xcb:
                                            skip(2);
                                            break;
                                        case 0xCC:
                                            skip(2);
                                            break;
                                        case 0xCF:
                                            skip(1);
                                            break;
                                        case 0xD0:
                                            skip(2);
                                            break;
                                        case 0xD1:
                                            skip(2);
                                            break;
                                        case 0xD2:
                                            skip(2);
                                            break;
                                        case 0xD5:
                                            skip(2);
                                            break;
                                        case 0xD8:
                                            skip(2);
                                            break;
                                        case 0xDA:
                                            skip(1);
                                            break;
                                        case 0xDB:
                                            skip(1);
                                            break;
                                        case 0xDD:
                                            skip(3);
                                            break;
                                        case 0xDF:
                                            skip(4);
                                            break;
                                        case 0xE0:
                                            skip(2);
                                            break;
                                        case 0xE2:
                                            skip(1);
                                            break;
                                        case 0xE3:
                                            skip(1);
                                            break;
                                        case 0xE6:
                                            skip(2);
                                            break;
                                        case 0xE7:
                                            skip(2);
                                            break;
                                        case 0xEF:
                                            skip(3);
                                            break; // DELX (vlq)
                                        case 0xF0:
                                            CurrentRot.readVlq();
                                            break;
                                        case 0xF1:
                                            skip(1);
                                            break;
                                        case 0xF4:
                                            skip(1);
                                            break;
                                        case 0xF9:
                                            skip(2);
                                            break;
                                        case 0xFD: // PPQC u16
                                            var trate = CurrentRot.getOpword();                                           
                                                Console.WriteLine("{1} PPQN change {0}", trate, routine);
                                                ppqn = trate;
                                                update_ticklen();
                                            break;
                                        case 0xFE: // BPMC u16
                                            var bpmx = CurrentRot.getOpword();
                                            Console.WriteLine("{1} BPM change {0}", bpmx, routine);
                                            bpm = bpmx;
                                            update_ticklen();
                                            break;
                                        case 0xFF: // HALT
                                            CurrentRot.stopped = true;
                                            break;
                                        default: // FALL 
                                            Console.WriteLine("{0} Unknown opcode 0x{1:X} ", routine, opcode);
                                            break;





                                    }
                                }

                            }

                        }
                    }
                    NOP(tick_len);
                }

            } catch (Exception E)
            {
                Console.WriteLine(E.ToString());
            }
        }
    }
}
