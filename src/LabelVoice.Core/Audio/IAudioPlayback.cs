using NAudio.Wave;

namespace LabelVoice.Core.Audio
{
    public interface IAudioPlayback
    {
        #region Properties

        int DeviceNumber { get; }

        #endregion Properties

        #region Methods

        void Init(ISampleProvider sampleProvider);

        void Play();

        void Pause();

        void Stop();

        void Dispose();

        PlaybackState GetPlaybackState();

        void SwitchDevice(Guid guid, int deviceNumber);

        List<AudioDevice> GetDevices();

        #endregion Methods
    }
}