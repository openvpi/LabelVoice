﻿using System.Runtime.InteropServices;

namespace LabelVoice.Core.Audio.PortAudio
{
    internal class PaAudioEngine
    {
        private const int FramesPerBuffer = 0; // paFramesPerBufferUnspecified
        private const PaBinding.PaStreamFlags StreamFlags = PaBinding.PaStreamFlags.paNoFlag;
        public readonly int channels;
        public readonly int sampleRate;
        public readonly double latency;
        private readonly IntPtr stream;
        private bool disposed;

        public readonly PaAudioDevice device;

        public PaAudioEngine(PaAudioDevice device, int channels, int sampleRate, double latency)
        {
            this.device = device;
            this.channels = channels;
            this.sampleRate = sampleRate;
            this.latency = latency;

            var parameters = new PaBinding.PaStreamParameters
            {
                channelCount = channels,
                device = device.DeviceIndex,
                hostApiSpecificStreamInfo = IntPtr.Zero,
                sampleFormat = PaBinding.PaSampleFormat.paFloat32,
                suggestedLatency = latency
            };

            IntPtr stream;

            unsafe
            {
                PaBinding.PaStreamParameters tempParameters;
                var parametersPtr = new IntPtr(&tempParameters);
                Marshal.StructureToPtr(parameters, parametersPtr, false);

                var code = PaBinding.Pa_OpenStream(
                    new IntPtr(&stream),
                    IntPtr.Zero,
                    parametersPtr,
                    sampleRate,
                    FramesPerBuffer,
                    StreamFlags,
                    null,
                    IntPtr.Zero
                );

                PaBinding.MaybeThrow(code);
            }

            this.stream = stream;

            PaBinding.MaybeThrow(PaBinding.Pa_StartStream(stream));
        }

        public void Send(Span<float> samples)
        {
            unsafe
            {
                fixed (float* buffer = samples)
                {
                    var frames = samples.Length / channels;
                    PaBinding.Pa_WriteStream(stream, (IntPtr)buffer, frames);
                }
            }
        }

        public void Dispose()
        {
            if (disposed || stream == IntPtr.Zero)
            {
                return;
            }
            PaBinding.Pa_AbortStream(stream);
            PaBinding.Pa_CloseStream(stream);
            disposed = true;
        }
    }
}