namespace LabelVoice.Core.Audio.PortAudio
{
    public sealed class PaAudioFrame
    {
        public PaAudioFrame(double presentationTime, float[] data)
        {
            PresentationTime = presentationTime;
            Data = data;
        }

        public double PresentationTime { get; }
        public float[] Data { get; }
    }
}