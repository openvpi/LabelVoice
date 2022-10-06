using LabelVoice.Core.Audio.PortAudio;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace LabelVoice.Core.Audio
{
    internal class PortAudioPlayback : IAudioPlayback, IDisposable
    {
        #region Properties

        public PlaybackState PlaybackState { get; private set; }
        public int DeviceNumber { get; private set; }

        #endregion Properties

        #region Private Fields

        private const int Channels = 2;
        private readonly object lockObj = new object();
        private ConcurrentQueue<PaAudioFrame> queue = new ConcurrentQueue<PaAudioFrame>();
        private PaAudioEngine audioEngine;
        private ISampleProvider sampleProvider;
        private float[] buffer;
        private double bufferedTimeMs;
        private double currentTimeMs;
        private Thread pushThread;
        private Thread pullThread;
        private bool eof;
        private bool shutdown;
        private bool disposed;

        #endregion Private Fields

        #region Constructors

        public PortAudioPlayback()
        {
            PaBinding.Pa_Initialize();

            buffer = new float[0];
            SwitchDevice(new Guid(), PaBinding.Pa_GetDefaultOutputDevice());

            pullThread = new Thread(Pull) { IsBackground = true, Priority = ThreadPriority.Highest };
            pushThread = new Thread(Push) { IsBackground = true, Priority = ThreadPriority.Highest };
            pullThread.Start();
            pushThread.Start();
        }

        #endregion Constructors

        #region Methods

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            PlaybackState = PlaybackState.Stopped;
            shutdown = true;

            if (pushThread != null)
            {
                while (pushThread.IsAlive)
                {
                    Thread.Sleep(10);
                }
                pushThread = null;
            }
            if (pullThread != null)
            {
                while (pullThread.IsAlive)
                {
                    Thread.Sleep(10);
                }
                pullThread = null;
            }
            queue.Clear();

            GC.SuppressFinalize(this);

            disposed = true;
        }

        public List<AudioDevice> GetDevices()
        {
            List<AudioDevice> devices = new List<AudioDevice>();
            int count = PaBinding.Pa_GetDeviceCount();
            PaBinding.MaybeThrow(count);
            for (int i = 0; i < count; ++i)
            {
                var device = GetEligibleOutputDevice(i);
                if (device is PaAudioDevice dev)
                {
                    devices.Add(new AudioDevice()
                    {
                        api = dev.HostApi,
                        name = dev.Name,
                        deviceNumber = dev.DeviceIndex,
                        guid = new Guid(),
                    });
                }
            }
            return devices;
        }

        public PlaybackState GetPlaybackState() => PlaybackState;

        public void Init(ISampleProvider sampleProvider)
        {
            PlaybackState = PlaybackState.Stopped;
            eof = false;
            queue.Clear();
            bufferedTimeMs = 0;
            currentTimeMs = 0;
            var sampleRate = audioEngine?.sampleRate ?? 44100;
            if (sampleRate != sampleProvider.WaveFormat.SampleRate)
            {
                sampleProvider = new WdlResamplingSampleProvider(sampleProvider, sampleRate);
            }
            this.sampleProvider = sampleProvider.ToStereo();
        }

        public void Pause()
        {
            PlaybackState = PlaybackState.Paused;
        }

        public void Play()
        {
            eof = false;
            queue.Clear();
            currentTimeMs = 0;
            PlaybackState = PlaybackState.Playing;
        }

        public void SwitchDevice(Guid guid, int deviceNumber)
        {
            lock (lockObj)
            {
                if (audioEngine == null || audioEngine.device.DeviceIndex != deviceNumber)
                {
                    var device = GetEligibleOutputDevice(deviceNumber);
                    if (device is PaAudioDevice dev)
                    {
                        audioEngine?.Dispose();
                        double latency = dev.DefaultHighOutputLatency;
                        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && latency < 0.1)
                        {
                            latency = 0.1;
                        }
                        audioEngine = new PaAudioEngine(
                            dev,
                            Channels,
                            dev.DefaultSampleRate,
                            latency);
                        DeviceNumber = deviceNumber;
                        buffer = new float[dev.DefaultSampleRate * Channels * 10 / 1000]; // 10ms at 44.1kHz
                    }
                }
            }
        }

        public void Stop()
        {
            PlaybackState = PlaybackState.Stopped;
            bufferedTimeMs = 0;
            currentTimeMs = 0;
            sampleProvider = null;
            queue.Clear();
        }

        private PaAudioDevice? GetEligibleOutputDevice(int index)
        {
            var device = new PaAudioDevice(PaBinding.GetDeviceInfo(index), index);
            if (device.MaxOutputChannels < Channels)
            {
                return null;
            }
            var api = device.HostApi.ToLowerInvariant();
            if (api.Contains("wasapi") || api.Contains("wdm-ks"))
            {
                return null;
            }
            var parameters = new PaBinding.PaStreamParameters
            {
                channelCount = Channels,
                device = device.DeviceIndex,
                hostApiSpecificStreamInfo = IntPtr.Zero,
                sampleFormat = PaBinding.PaSampleFormat.paFloat32,
            };
            unsafe
            {
                int code = PaBinding.Pa_IsFormatSupported(IntPtr.Zero, new IntPtr(&parameters), 44100);
                if (code < 0)
                {
                    return null;
                }
            }
            return device;
        }

        private void Push()
        {
            while (!shutdown)
            {
                if (PlaybackState == PlaybackState.Paused ||
                    PlaybackState == PlaybackState.Stopped)
                {
                    Thread.Sleep(10);
                    continue;
                }

                PaAudioEngine engine = audioEngine;
                if (engine == null)
                {
                    Thread.Sleep(10);
                    continue;
                }

                if (queue.Count == 0)
                {
                    if (eof)
                    {
                        PlaybackState = PlaybackState.Stopped;
                        Thread.Sleep(10);
                        continue;
                    }
                    Thread.Sleep(10);
                    continue;
                }

                if (!queue.TryDequeue(out var frame))
                {
                    Thread.Sleep(10);
                    continue;
                }

                if (PlaybackState != PlaybackState.Playing)
                {
                    PlaybackState = PlaybackState.Playing;
                }
                engine.Send(frame.Data);
                currentTimeMs = frame.PresentationTime;
            }
        }

        private void Pull()
        {
            while (!shutdown)
            {
                var sp = sampleProvider;
                if (sp == null)
                {
                    Thread.Sleep(10);
                    continue;
                }
                if (PlaybackState == PlaybackState.Paused ||
                    PlaybackState == PlaybackState.Stopped)
                {
                    Thread.Sleep(10);
                    continue;
                }
                if (queue.Count >= 10)
                {
                    Thread.Sleep(10);
                    continue;
                }

                var n = sp.Read(buffer, 0, buffer.Length);
                if (n == 0)
                {
                    eof = true;
                    Thread.Sleep(10);
                    continue;
                }
                var data = new float[n];
                Array.Copy(buffer, data, n);
                var frame = new PaAudioFrame(bufferedTimeMs, data);
                queue.Enqueue(frame);
                var sampleRate = audioEngine?.sampleRate ?? 44100;
                bufferedTimeMs += n * 1000.0 / sampleRate / Channels;
            }
        }

        #endregion Methods
    }
}