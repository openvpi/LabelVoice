﻿using NAudio.Wave;

namespace LabelVoice.Core.Audio
{
    internal class PortAudioPlayback : IAudioPlayback, IDisposable
    {
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public long GetLength()
        {
            throw new NotImplementedException();
        }

        public PlaybackState GetPlaybackState()
        {
            throw new NotImplementedException();
        }

        public long GetPosition()
        {
            throw new NotImplementedException();
        }

        public void Init(ISampleProvider sampleProvider)
        {
            throw new NotImplementedException();
        }

        public void Pause()
        {
            throw new NotImplementedException();
        }

        public void Play()
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }
    }
}
