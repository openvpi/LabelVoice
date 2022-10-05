using NAudio.Wave;

namespace LabelVoice.Core.Audio
{
    public interface IAudioPlayback
    {
        //void Load(string fileName);
        void Init(ISampleProvider sampleProvider);

        void Play();

        void Pause();

        void Stop();

        void Dispose();

        PlaybackState GetPlaybackState();

        //TimeSpan GetCurrentTime();
        //void SetCurrentTime(TimeSpan time);
        //TimeSpan GetTotalTime();
        long GetPosition();

        //void SetPosition(long pos);
        long GetLength();
    }
}