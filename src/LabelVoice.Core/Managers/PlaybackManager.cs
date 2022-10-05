using LabelVoice.Core.Audio;
using LabelVoice.Core.Utils;
using NAudio.Wave;

namespace LabelVoice.Core.Managers
{
    /// <summary>
    /// Provides a range of methods to control audio playback.
    /// </summary>
    public class PlaybackManager : SingletonBase<PlaybackManager>
    {
        #region Private Fields

        private readonly AudioPlayback _audioPlayback = new();

        #endregion Private Fields

        #region Methods

        public void Load(string path) => _audioPlayback.Load(path);

        public void Play() => _audioPlayback.Play();

        public void Pause() => _audioPlayback.Pause();

        public void Stop() => _audioPlayback.Stop();

        public PlaybackState GetPlaybackState() => _audioPlayback.GetPlaybackState();

        public TimeSpan GetCurrentTime() => _audioPlayback.GetCurrentTime();

        public void SetCurrentTime(TimeSpan time) => _audioPlayback.SetCurrentTime(time);

        public TimeSpan GetTotalTime() => _audioPlayback.GetTotalTime();

        public long GetPosition() => _audioPlayback.GetPosition();

        public void SetPosition(long pos) => _audioPlayback.SetPosition(pos);

        public long GetLength() => _audioPlayback.GetLength();

        public void Dispose() => _audioPlayback.Dispose();

        #endregion Methods
    }
}