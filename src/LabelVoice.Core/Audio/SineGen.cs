using NAudio.Wave;

namespace LabelVoice.Core.Audio
{
    public class SineGen : ISampleProvider
    {
        public WaveFormat WaveFormat => waveFormat;
        public double Freq { get; set; }
        public bool Stop { get; set; }
        private WaveFormat waveFormat;
        private double phase;
        private double gain;
        public SineGen()
        {
            waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 1);
            Freq = 440;
            gain = 1;
        }
        public int Read(float[] buffer, int offset, int count)
        {
            double delta = 2 * Math.PI * Freq / waveFormat.SampleRate;
            for (int i = 0; i < count; i++)
            {
                if (Stop)
                {
                    gain = Math.Max(0, gain - 0.01);
                }
                if (gain == 0)
                {
                    return i;
                }
                phase += delta;
                double sampleValue = Math.Sin(phase) * 0.2 * gain;
                buffer[offset++] = (float)sampleValue;
            }
            return count;
        }
    }
}
