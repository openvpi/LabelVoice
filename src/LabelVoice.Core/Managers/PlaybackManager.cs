using FFmpeg.AutoGen;
using LabelVoice.Core.Audio;
using LabelVoice.Core.Utils;
using NAudio.Extras;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Test.Playback;

namespace LabelVoice.Core.Managers
{
    /// <summary>
    /// Provides a range of methods to control audio playback.
    /// </summary>
    public class PlaybackManager : SingletonBase<PlaybackManager>, IDisposable
    {
        #region Properties

        public AudioDecoder AudioDecoder { get; private set; } = AudioDecoder.NAudio;

        #endregion Properties

        #region Private Fields

        private IAudioPlayback? _audioPlayback;

        private WaveStream? _fileStream;

        private string? _audioFilePath;

        private long _lastPosition = 0;

        private PlaybackState _lastPlayState = PlaybackState.Stopped;

        private AudioBackend _audioBackend;

        #endregion Private Fields

        #region Methods

        /// <summary>
        /// Load an audio file. (Wave PCM, Wave IEE Float, MP3 and AIFF)
        /// </summary>
        /// <param name="audioFilePath"></param>
        public void Load(string audioFilePath)
        {
            _audioFilePath = audioFilePath;
            Stop();
            _fileStream?.Dispose();
            _fileStream = null;
            ISampleProvider inputStream;
            switch (AudioDecoder)
            {
                case AudioDecoder.FFmpeg:
                    //ffmpeg.RootPath = @"D:\软件\实用\ffmpeg\dll"; // 改成你放FFmpeg动态库的目录
                    inputStream = new FFmpegWaveProvider(audioFilePath,
                        new FFmpegWaveProvider.WaveArguments(44100, AVSampleFormat.AV_SAMPLE_FMT_FLT, 2));
                    break;

                case AudioDecoder.NAudio:
                default:
                    inputStream = new LvAudioFileReader(audioFilePath);
                    break;
            }
            _fileStream = (WaveStream?)inputStream;
            SampleAggregator? aggregator = new(inputStream);
            Init(aggregator);
        }

        private void Reload()
        {
            if (_audioFilePath == null)
            {
                return;
            }
            Pause();
            Load(_audioFilePath);
        }

        /// <summary>
        /// Initialize the playback manager.
        /// </summary>
        /// <param name="sampleProvider"></param>
        public void Init(ISampleProvider sampleProvider)
        {
            _audioPlayback?.Init(sampleProvider);
        }

        public void SwitchAudioDecoder(AudioDecoder decoder)
        {
            SavePlaybackStatus();
            Stop();
            AudioDecoder = decoder;
            Reload();
            RestorePlaybackStatus();
        }

        /// <summary>
        /// Set audio backend.
        /// </summary>
        /// <param name="backend"></param>
        public void SetAudioBackend(AudioBackend backend)
        {
            _audioPlayback = backend switch
            {
                AudioBackend.NAudio => new NAudioPlayback(),
                AudioBackend.PortAudio => new PortAudioPlayback(),
                AudioBackend.SDL => new SDLPlayback(),
                _ => null,
            };
            _audioBackend = backend;
        }

        public void PlayTestSound()
        {
            SavePlaybackStatus();
            Stop();
            Init(new SignalGenerator(44100, 1).Take(TimeSpan.FromSeconds(1)));
            Play();
            Thread.Sleep(2000);
            Reload();
            RestorePlaybackStatus();
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

        /// <summary>
        /// Get the position within the current stream.
        /// </summary>
        /// <returns></returns>
        public long GetPosition() =>
            _fileStream != null
                ? _fileStream.Position
                : 0;

        /// <summary>
        /// Set the position within the current stream.
        /// </summary>
        /// <param name="pos"></param>
        public void SetPosition(long pos)
        {
            if (_fileStream != null)
                _fileStream.Position = pos;
        }

        /// <summary>
        /// Get the length in bytes of the stream.
        /// </summary>
        /// <returns></returns>
        public long GetLength() =>
            _fileStream != null
                ? _fileStream.Length
                : 0;

        /// <summary>
        /// Dispose the playback manager.
        /// </summary>
        public void Dispose()
        {
            _audioPlayback?.Dispose();
            _fileStream?.Dispose();
            GC.SuppressFinalize(this);
        }

        public int? GetDeviceNumber() => _audioPlayback?.DeviceNumber;

        /// <summary>
        /// Get audio playback devices.
        /// </summary>
        /// <returns></returns>
        public List<AudioDevice> GetDevices() =>
            _audioPlayback != null
                ? _audioPlayback.GetDevices()
                : new List<AudioDevice>();

        /// <summary>
        /// Switch audio playback device by guid.
        /// </summary>
        /// <param name="guid"></param>
        public void SwitchDevice(Guid guid)
        {
            SavePlaybackStatus();
            Stop();
            //NAudio needs to set device before initialization.
            if (_audioBackend == AudioBackend.NAudio)
                _audioPlayback?.SwitchDevice(guid, 0);
            Reload();
            //SDL needs to set device after initialization, because once initialized, the device will return to the default.
            if (_audioBackend == AudioBackend.SDL)
                _audioPlayback?.SwitchDevice(guid, 0);
            RestorePlaybackStatus();
        }

        /// <summary>
        /// Switch audio playback device by device number. (recommend)
        /// </summary>
        /// <param name="deviceNumber"></param>
        public void SwitchDevice(int deviceNumber)
        {
            SavePlaybackStatus();
            Stop();
            //NAudio needs to set device before initialization.
            if (_audioBackend == AudioBackend.NAudio)
                _audioPlayback?.SwitchDevice(new Guid(), deviceNumber);
            Reload();
            //SDL needs to set device after initialization, because once initialized, the device will return to the default.
            if (_audioBackend == AudioBackend.SDL)
                _audioPlayback?.SwitchDevice(new Guid(), deviceNumber);
            RestorePlaybackStatus();
        }

        /// <summary>
        /// Switch audio playback device by both guid and device number.
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="deviceNumber"></param>
        public void SwitchDevice(Guid guid, int deviceNumber)
        {
            SavePlaybackStatus();
            Stop();
            //NAudio needs to set device before initialization.
            if (_audioBackend == AudioBackend.NAudio)
                _audioPlayback?.SwitchDevice(guid, deviceNumber);
            Reload();
            //SDL needs to set device after initialization, because once initialized, the device will return to the default.
            if (_audioBackend == AudioBackend.SDL)
                _audioPlayback?.SwitchDevice(guid, deviceNumber);
            RestorePlaybackStatus();
        }

        private void SavePlaybackStatus()
        {
            _lastPlayState = GetPlaybackState();
            _lastPosition = GetPosition();
        }

        private void RestorePlaybackStatus()
        {
            SetPosition(_lastPosition);
            switch (_lastPlayState)
            {
                case PlaybackState.Playing:
                    Play();
                    break;

                case PlaybackState.Paused:
                    Pause();
                    break;
            }
        }

        #endregion Methods
    }
}