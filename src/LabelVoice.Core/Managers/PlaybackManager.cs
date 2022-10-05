using LabelVoice.Core.Audio;
using LabelVoice.Core.Utils;
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

        //private ISampleProvider _audioSampleProvider;

        #endregion Private Fields

        #region Methods

        //public void Load(string path) => _audioPlayback.Load(path);

        public void Init(ISampleProvider sampleProvider) => _audioPlayback?.Init(sampleProvider);

        public void SetAudioBackend(AudioBackend backend)
        {
            _audioPlayback = backend switch
            {
                AudioBackend.NAudio => new NAudioPlayback(),
                AudioBackend.PortAudio => new PortAudioPlayback(),
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

        public void Stop() => _audioPlayback?.Stop();

        public PlaybackState GetPlaybackState() =>
            _audioPlayback != null
            ? _audioPlayback.GetPlaybackState()
            : PlaybackState.Stopped;

        //public TimeSpan GetCurrentTime() => _audioPlayback.GetCurrentTime();

        //public void SetCurrentTime(TimeSpan time) => _audioPlayback.SetCurrentTime(time);

        //public TimeSpan GetTotalTime() => _audioPlayback.GetTotalTime();

        public long GetPosition() =>
            _audioPlayback != null
            ? _audioPlayback.GetPosition()
            : 0;

        //public void SetPosition(long pos) => _audioPlayback.SetPosition(pos);

        public long GetLength() =>
            _audioPlayback != null
            ? _audioPlayback.GetLength()
            : 0;

        public void Dispose() => _audioPlayback?.Dispose();

        #endregion Methods
    }
}