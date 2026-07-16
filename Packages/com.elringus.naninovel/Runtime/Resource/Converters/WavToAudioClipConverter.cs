using System;
using System.IO;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Converts <see cref="T:byte[]"/> raw data of a .wav audio file to <see cref="AudioClip"/>.
    /// Only PCM16 44100Hz stereo .wav are supported.
    /// </summary>
    public class WavToAudioClipConverter : IResourceConverter
    {
        public bool Supports (string extension)
        {
            return extension == ".wav";
        }

        public UnityEngine.Object Convert (byte[] bytes, string fullPath)
        {
            var data = Pcm16ToFloatArray(bytes);
            var clip = AudioClip.Create("Generated WAV Audio", data.Length / 2, 2, 44100, false);
            clip.name = Path.GetFileName(fullPath);
            clip.SetData(data, 0);
            return clip;
        }

        private static float[] Pcm16ToFloatArray (byte[] input)
        {
            // PCM16 wav usually has 44 byte headers, though not always. 
            // https://stackoverflow.com/questions/19991405/how-can-i-detect-whether-a-wav-file-has-a-44-or-46-byte-header
            const int headerSize = 444;
            var inputSamples = input.Length / 2; // 16 bit input, so 2 bytes per sample.
            var output = new float[inputSamples];
            var outputIndex = 0;
            for (var n = headerSize; n < inputSamples; n++)
            {
                var sample = BitConverter.ToInt16(input, n * 2);
                output[outputIndex++] = sample / 32768f;
            }
            return output;
        }
    }
}
