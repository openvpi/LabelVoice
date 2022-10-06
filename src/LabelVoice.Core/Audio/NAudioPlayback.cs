using NAudio.Extras;
using NAudio.Wave;

namespace LabelVoice.Core.Audio
{
    public class NAudioPlayback : IAudioPlayback, IDisposable
    {
        #region Private Fields

        private IWavePlayer? _playbackDevice;

        private WaveStream? _fileStream;

        private int _deviceNumber;

        #endregion Private Fields

        #region Properties

        public int DeviceNumber => _deviceNumber;

        #endregion Properties

        #region Events

        public event EventHandler<FftEventArgs>? FftCalculated;

        public event EventHandler<MaxSampleEventArgs>? MaximumCalculated;

        #endregion Events

        #region Methods

        private void EnsureDeviceCreated()
        {
            if (_playbackDevice == null)
                CreateDevice();
        }

        private void CreateDevice()
        {
            //_playbackDevice = new WaveOut { DesiredLatency = 200 };
            _playbackDevice = new WaveOutEvent
            {
                DeviceNumber = _deviceNumber,
            };
        }

        public void Play()
        {
            EnsureDeviceCreated();
            if (_playbackDevice != null
                //&& _fileStream != null
                && _playbackDevice.PlaybackState != PlaybackState.Playing)
                _playbackDevice.Play();
        }

        public void Pause()
        {
            EnsureDeviceCreated();
            _playbackDevice?.Pause();
        }

        public void Stop()
        {
            _playbackDevice?.Stop();
        }

        public void Dispose()
        {
            Stop();
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

        public void SwitchDevice(Guid guid, int deviceNumber)
        {
            _deviceNumber = deviceNumber - 1;
            Dispose();
        }

        public List<AudioDevice> GetDevices()
        {
            var devices = new List<AudioDevice>();
            int i = 0;
            foreach (var device in DirectSoundOut.Devices)
            {
                devices.Add(new AudioDevice
                {
                    api = "DirectSoundOut",
                    name = device.Description,
                    deviceNumber = i,
                    guid = device.Guid,
                });
                i++;
            }
            return devices;
        }
    }

    #endregion Methods
}