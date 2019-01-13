using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BMSXPX.AAF.Types;
using BMSXPX.AAF;
using System.IO;
using XAYRGA.SharpSL;


namespace BMSXPX.BMS
{
    class BMSAudioSample
    {
        public WSYSWave WaveData;
        public SoundEffect Sample;
    }


    class BMSAudioEngine
    {


          
        
        static double FrequencyRatioToCents(double freqRatio)
        {
            return Math.Round(1000000 * 1200 * Math.Log(freqRatio,2)) / 1000000.0;
        }

        BMSAudioSample[][] SampleBank;
        Dictionary<int, Instrument>[] InstrumentBanks;
        Dictionary<int, BMSAudioChannel> Channels; 
        
        AAFFile ResourceData; 
        public BMSAudioEngine(AAFFile AAFData)
        {

            SharpSLEngine.Init(); 
            Channels = new Dictionary<int, BMSAudioChannel>();
            ResourceData = AAFData;
            SampleBank = new BMSAudioSample[32][];
            InstrumentBanks = new Dictionary<int, Instrument>[256];  // Here's to hoping we don't have more than 256 ibnks. 
            for (int i=0; i < 32;i++)
            {
                SampleBank[i] = new BMSAudioSample[1024];  // Preallocate the sample banks. 
            }
            
         
  
        }



        public bool AddChannel(int id)
        {
            Channels[id] = new BMSAudioChannel();

            return true;
        }


        public bool ChangeChannelBank(int chn, int bnk)
        {
            Channels[chn].Bank = bnk;

            return true;
        }

        public bool ChangeChannelProgram(int chn, int prgm)
        {
            Channels[chn].Program = prgm;

            return true;
        }

        public bool StartSound(int chn ,int note, int voice, int vel)
        {
            var Param = Channels[chn];
      
            var IBNK = InstrumentBanks[Param.Bank];
            if (IBNK==null) {


                Console.WriteLine("NULL IBNK");

                return false;
            };

            if (voice > 16)
            {
                Console.WriteLine("START VOICE OUT OF RANGE: {0}", voice);
                return false; 
            }
            Instrument Inst;

            IBNK.TryGetValue(Param.Program,out Inst);


            if (Inst==null) {
                Console.WriteLine("NULL Instrument");

                return false; };
            var CKeyx = Inst.Keys[note];
            if (CKeyx == null)
            {
                Console.WriteLine("note not found for inst. BANK: {0} Program: {1} Note: {2} Vel: {3} ", Param.Bank, Param.Program, note, vel);
                return false;
            }
            var CKey = Inst.Keys[note][vel];
            // Console.WriteLine("Play BANKID: {0} N: {1} V: {2}", Param.Bank, note, vel);
            if (CKey==null)
            {
                Console.WriteLine("vTable index not found Bank: {0} Program: {1} Note: {2} Vel: {3}",Param.Bank,Param.Program,note,vel);
                return false;
            }
            var WaveID = CKey.wave;
            var WSysID = CKey.wsysid;
            

            var SampData = SampleBank[WSysID][WaveID];

            var sound = SampData.Sample;

            var waveParams = SampData.WaveData;

            var VMul = Inst.Volume * CKey.Volume;

            var PMul = Inst.Pitch * CKey.Pitch;

            var wsyswave = SampData.WaveData;

            var UnityKey = wsyswave.key;
            

            var VelMul = CKey.velocity;

            // Console.WriteLine(UnityKey);

            //Console.ReadLine();

            try
            {

                var store = sound.CreateInstance();




                // Math.Pow(2, semitones / 12);
                var true_volume = (Math.Pow(((float)vel) / 127, 2) * VMul) * 0.5;
                var real_pitch = Math.Pow(2, ((note - UnityKey) * PMul) / 12);
                if (Inst.percussion)
                {
                    store.Pitch = 7f;
                    store.Volume = (float)true_volume;

                }
                else
                {
                    store.Pitch = (float)(real_pitch);


                    //Console.WriteLine("AA: {0} {1} {2} {3}", VMul, vel, wtf, voice);
                    store.Volume = (float)true_volume;
                }





                Param.AddVoice(store, voice);

                store.Play();

            } catch (Exception Q)
            {
                Console.WriteLine(Q.ToString());
            }


            return true;
        }

        public bool StopSound(int chn, int voice)
        {

           // Console.WriteLine("STOP: {0} - {1}", chn, voice);
            var Param = Channels[chn];
            Param.StopVoice(voice);


            return true; 
        }


        public bool preloadSample(int wsysid,int sampid)
        {   

            if (SampleBank[wsysid][sampid]!=null)
            {
                return false; 
            }

                var samp = ResourceData.WaveSystems[wsysid].SampleMap[sampid];

            var NewSample = new BMSAudioSample();

            NewSample.WaveData = samp;

            var sdata = new SoundEffect(samp.pcmpath,samp.loop,(int)samp.loop_start,(int)samp.loop_end);
            // File.Copy(samp.pcmpath, "./preload_/" + samp.id + ".wav");
            NewSample.Sample = sdata;
            Console.WriteLine("LOADED SAMPLE {0}", samp.pcmpath);


    
            SampleBank[wsysid][sampid] = NewSample;

            return true; 
        }

        public bool preloadInst(int bank, int prgm)
        {
            if (InstrumentBanks[bank]!=null)
            {
                var ibk = InstrumentBanks[bank];
                Instrument bout;
                ibk.TryGetValue(prgm, out bout);
                if (bout!=null)
                {
                    return false; 
                }
            }
            var InstData = ResourceData.InstrumentBanks[bank].Instruments[prgm];
            Console.WriteLine("LOADED INSTRUMENT B:{0} P:{1}", bank,prgm);
            if (InstrumentBanks[bank]==null)
            {
                InstrumentBanks[bank] = new Dictionary<int, Instrument>();
            }
            if (InstData==null)
            {
                return false;
            }
          //  Console.WriteLine("Instrument has {0} keys", InstData.Keys.Length);
            for (int i=0; i < InstData.Keys.Length; i++)
            {

                var C_IKeyh = InstData.Keys[i];
                if (C_IKeyh != null) {
                    //Console.WriteLine("non-null key. ");
                    for (int vri = 0; vri < C_IKeyh.Length; vri++)
                    {
                        
                        var VelRegion = C_IKeyh[vri];
                        if (VelRegion!=null)
                        {
                            preloadSample((int)VelRegion.wsysid, (int)VelRegion.wave);
                        }
                    }
               }

            }
            InstrumentBanks[bank][prgm] = InstData;
            return true; 
        }

    }
}
