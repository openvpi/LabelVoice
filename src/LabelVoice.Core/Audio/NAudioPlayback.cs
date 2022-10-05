using NAudio.Extras;
using NAudio.Wave;

namespace LabelVoice.Core.Audio
{
    public class NAudioPlayback : IAudioPlayback, IDisposable
    {
        #region Private Fields

        private IWavePlayer? _playbackDevice;

        private WaveStream? _fileStream;

        #endregion Private Fields

        #region Properties

        public int DeviceNumber => throw new NotImplementedException();

        #endregion Properties

        #region Events

        public event EventHandler<FftEventArgs>? FftCalculated;

        public event EventHandler<MaxSampleEventArgs>? MaximumCalculated;

        #endregion Events

        #region Methods

        private void CloseFile()
        {
            _fileStream?.Dispose();
            _fileStream = null;
        }

        private void EnsureDeviceCreated()
        {
            if (_playbackDevice == null)
                CreateDevice();
        }

        private void CreateDevice()
        {
            //_playbackDevice = new WaveOut { DesiredLatency = 200 };
            _playbackDevice = new WaveOutEvent();
        }

        public void Play()
        {
            if (_playbackDevice != null
                //&& _fileStream != null
                && _playbackDevice.PlaybackState != PlaybackState.Playing)
                _playbackDevice.Play();
        }

        public void Pause()
        {
            _playbackDevice?.Pause();
        }

        public void Stop()
        {
            _playbackDevice?.Stop();
        }

        public void Dispose()
        {
            Stop();
            CloseFile();
            _playbackDevice?.Dispose();
            _playbackDevice = null;
        }

        public PlaybackState GetPlaybackState() =>
            _playbackDevice != null
            ? _playbackDevice.PlaybackState
            : PlaybackState.Stopped;

        public void Init(ISampleProvider sampleProvider)
        {
            Stop();
            EnsureDeviceCreated();
            _playbackDevice.Init(sampleProvider);
        }

        public void SelectDevice(Guid guid, int deviceNumber)
        {
            throw new NotImplementedException();
        }

        public List<AudioDevice> GetDevices()
        {
            throw new NotImplementedException();
        }
    }

    #endregion Methods
}