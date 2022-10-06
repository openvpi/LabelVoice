using LabelVoice.Core.Audio;
using LabelVoice.Core.Utils;
using NAudio.Extras;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace LabelVoice.Core.Managers
{
    /// <summary>
    /// Provides a range of methods to control audio playback.
    /// </summary>
    public class PlaybackManager : SingletonBase<PlaybackManager>
    {
        #region Private Fields

        private IAudioPlayback? _audioPlayback;

        private WaveStream? _fileStream;

        #endregion Private Fields

        #region Methods

        //public void Load(string path) => _audioPlayback.Load(path);

        public void Load(string audioFilePath)
        {
            Stop();
            _fileStream?.Dispose();
            _fileStream = null;
            LvAudioFileReader inputStream = new(audioFilePath);
            _fileStream = inputStream;
            SampleAggregator? aggregator = new(inputStream);
            Init(aggregator);
        }

        public void Init(ISampleProvider sampleProvider)
        {
            _audioPlayback?.Init(sampleProvider);
        }

        public void SetAudioBackend(AudioBackend backend)
        {
            _audioPlayback = backend switch
            {
                AudioBackend.NAudio => new NAudioPlayback(),
                AudioBackend.PortAudio => new PortAudioPlayback(),
                AudioBackend.SDL => new SDLPlayback(),
                _ => null,
            };
        }

        public void PlayTestSound()
        {
            Stop();
            Init(new SignalGenerator(44100, 1).Take(TimeSpan.FromSeconds(1)));
            Play();
        }

        public void Play() => _audioPlayback?.Play();

        public void Pause() => _audioPlayback?.Pause();

        public void Stop()
        {
            _audioPlayback?.Stop();
            if (_fileStream != null)
            {
                _fileStream.Position = 0;
            }
        }

        public PlaybackState GetPlaybackState() =>
            _audioPlayback != null
                ? _audioPlayback.GetPlaybackState()
                : PlaybackState.Stopped;

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
            _audioPlayback?.Dispose();
            _fileStream?.Dispose();
        }

        public List<AudioDevice> GetAudioDevices() =>
            _audioPlayback != null
            ? _audioPlayback.GetDevices()
            : new List<AudioDevice>();

        #endregion Methods
    }
}