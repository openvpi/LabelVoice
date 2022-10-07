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

        public ushort SampleFormat;

        public int Channels;

        public int BufferSize;
    }
}