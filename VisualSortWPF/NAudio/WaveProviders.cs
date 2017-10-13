using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;

namespace NAudio.Wave
{
    public class SineWaveProvider32 : WaveProvider32
    {
        int sample;

        private float[] Frequencies { get; set; }
        public float Amplitude { get; set; }

        private ConcurrentQueue<float> _Frequencies;

        public SineWaveProvider32()
        {
            Amplitude = 0.25f;
            MaxFrequencies = 5;
            _Frequencies = new ConcurrentQueue<float>(); 
        }

        public int MaxFrequencies { get; set; }

        public void AddFrequencies(float[] frequencies)
        {
            //Frequencies = frequencies;
            for (int i = frequencies.Length - 1; i >= 0; i--)
                _Frequencies.Enqueue(frequencies[i]);
            //if (_Frequencies.Count > MaxFrequencies)
            //    _Frequencies.RemoveRange(0, _Frequencies.Count - MaxFrequencies);
        }

        public override int Read(float[] buffer, int offset, int sampleCount)
        {
            int sampleRate = WaveFormat.SampleRate;

            for (int n = 0; n < sampleCount; n++)
            {
                float sum = 0;
                List<float> frequencies = new List<float>();
                while (frequencies.Count < _Frequencies.Count && frequencies.Count < MaxFrequencies)
                {
                    float f;
                    if (_Frequencies.TryDequeue(out f))
                    {
                        frequencies.Add(f);
                        sum += (float)(Amplitude * Math.Sin((2 * Math.PI * sample * f) / sampleRate));
                    }
                }
                if (_Frequencies.Count < MaxFrequencies)
                {
                    while (_Frequencies.Count < MaxFrequencies && frequencies.Count > 0)
                    {
                        _Frequencies.Enqueue(frequencies[0]);
                        frequencies.RemoveAt(0);
                    }
                }
                buffer[n + offset] = sum / (float)_Frequencies.Count;
                sample++;
                if (sample >= sampleRate) sample = 0;
            }

            return sampleCount;
        }
    }

    public class SawtoothWaveProvider32 : WaveProvider32
    {
        int[] samples;

        private float[] _Frequencies;

        public float[] Frequencies
        {
            get { return _Frequencies; }
            set
            {
                _Frequencies = value;
                samples = new int[_Frequencies.Length];
            }
        }
        public float Amplitude { get; set; }

        public SawtoothWaveProvider32()
        {
            Amplitude = 0.25f;           
        }

        public override int Read(float[] buffer, int offset, int sampleCount)
        {
            int sampleRate = WaveFormat.SampleRate;
            int[] samplesPerWavelength = new int[_Frequencies.Length];
            float[] ampSteps = new float[_Frequencies.Length];
            for (int i = 0; i < samplesPerWavelength.Length; i++)
            {
                samplesPerWavelength[i] = Convert.ToInt32(sampleRate / (Frequencies[i] / WaveFormat.Channels));
                ampSteps[i] = (float)((Amplitude * 2) / (samplesPerWavelength[i] - 1));
            }

            for (int i = 0; i < sampleCount; i++)
            {
                float sum = 0;
                for (int j = 0; j < _Frequencies.Length; j++)
                {
                    sum += samples[j] * ampSteps[j] - Amplitude;

                    samples[j]++;
                    if (samples[j] >= samplesPerWavelength[j]) samples[j] = 0;
                }
                buffer[i + offset] = sum / (float)_Frequencies.Length;
            }

            return sampleCount;
        }
    }

    public class TriangleWaveProvider32 : WaveProvider32
    {
        int sample;

        public float Frequency { get; set; }
        public float Amplitude { get; set; }

        public TriangleWaveProvider32()
        {
            Frequency = 1000;
            Amplitude = 0.25f; // let's not hurt our ears            
        }

        public override int Read(float[] buffer, int offset, int sampleCount)
        {
            int sampleRate = WaveFormat.SampleRate / 2;
            int samplesPerWavelength = Convert.ToInt32(sampleRate / (Frequency / WaveFormat.Channels));
            float ampStep = (float)((Amplitude * 2) / samplesPerWavelength);
            bool inverse = false;
            float[] buffer2 = new float[buffer.Length];

            for (int i = 0; i < sampleCount; i++)
            {
                if (inverse)
                    buffer[i + offset] = Amplitude * 2 - sample * ampStep - Amplitude;
                else
                    buffer[i + offset] = sample * ampStep - Amplitude;

                buffer2[i + offset] = buffer[i + offset];

                sample++;
                if (sample >= samplesPerWavelength)
                {
                    sample = 0;
                    inverse = !inverse;
                }
            }

            return sampleCount;
        }
    }
}
