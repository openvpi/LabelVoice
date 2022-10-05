using NAudio.Extras;
using NAudio.Wave;

namespace LabelVoice.Core.Audio
{
    public class AudioPlayback : IDisposable
    {
        #region Private Fields

        private IWavePlayer? _playbackDevice;

        private WaveStream? _fileStream;

        #endregion Private Fields

        #region Events

        public event EventHandler<FftEventArgs>? FftCalculated;

        public event EventHandler<MaxSampleEventArgs>? MaximumCalculated;

        #endregion Events

        #region Methods

        public void Load(string fileName)
        {
            Stop();
            CloseFile();
            EnsureDeviceCreated();
            OpenFile(fileName);
        }

        private void CloseFile()
        {
            _fileStream?.Dispose();
            _fileStream = null;
        }

        private void OpenFile(string fileName)
        {
            try
            {
                AudioFileReader inputStream = new(fileName);
                _fileStream = inputStream;
                SampleAggregator? aggregator = new(inputStream)
                {
                    NotificationCount = inputStream.WaveFormat.SampleRate / 100,
                    PerformFFT = true
                };
                aggregator.FftCalculated += (s, a) => FftCalculated?.Invoke(this, a);
                aggregator.MaximumCalculated += (s, a) => MaximumCalculated?.Invoke(this, a);
                _playbackDevice.Init(aggregator);
            }
            catch (Exception e)
            {
                CloseFile();
            }
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
                && _fileStream != null
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
            if (_fileStream != null)
            {
                _fileStream.Position = 0;
            }
        }

        public TimeSpan GetCurrentTime() =>
            _fileStream != null
            ? _fileStream.CurrentTime
            : TimeSpan.Zero;

        public void SetCurrentTime(TimeSpan time)
        {
            if (_fileStream != null)
                _fileStream.CurrentTime = time;
        }

        public TimeSpan GetTotalTime() =>
            _fileStream != null
            ? _fileStream.TotalTime
            : TimeSpan.Zero;

        public long GetPosition() =>
            _fileStream != null
            ? _fileStream.Position
            : 0;

        public void SetPosition(long pos)
        {
            if (_fileStream != null)
                _fileStream.Position = pos;
        }

        public long GetLength() =>
            _fileStream != null
            ? _fileStream.Length
            : 0;

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
    }

    #endregion Methods
}