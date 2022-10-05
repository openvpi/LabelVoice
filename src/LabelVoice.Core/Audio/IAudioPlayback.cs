using NAudio.Wave;

namespace LabelVoice.Core.Audio
{
    public interface IAudioPlayback
    {
        #region Properties

        int DeviceNumber { get; }

        #endregion Properties

        #region Methods

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

        void SelectDevice(Guid guid, int deviceNumber);

        List<AudioDevice> GetDevices();

        #endregion Methods
    }
}