namespace LabelVoice.Core.Audio
{
    public enum AudioBackend
    {
        NAudio,
        PortAudio,
        SDL,
    }

    public struct AudioSpec
    {
        public int SampleRate;

        public int BufferSize;
    }
}