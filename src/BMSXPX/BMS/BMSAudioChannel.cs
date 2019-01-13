using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using BMSXPX.AAF;
using System.IO;
using XAYRGA.SharpSL; 

namespace BMSXPX.BMS
{
    class BMSAudioChannel
    {
        public int Bank = 0;
        public int Program = 0;
        SoundEffectInstance[] Voices;
        public BMSAudioChannel()
        {
            Voices = new SoundEffectInstance[10]; // Should only use 8, but just to be safe we'll make it 10. 
        }
        public bool AddVoice(SoundEffectInstance Push,int vid)
        {
            StopVoice(vid); // check to make sure we don't currently have one playing. 

            Voices[vid] = Push; 

            return true; // No reason it should fail, i hope.  Status code, just in case. 

        }

        public bool StopVoice(int vid)
        {
            var ReqVoice = Voices[vid];
            if (ReqVoice != null)
            {
                ReqVoice.Stop();
                ReqVoice.Dispose();
                Voices[vid] = null; 

                return true;
            }
            

            return false; // Exceptionless failure, false status code. 

        }


    }
}
