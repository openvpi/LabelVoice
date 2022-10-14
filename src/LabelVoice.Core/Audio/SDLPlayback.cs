using System.Runtime.InteropServices;
using NAudio.Wave;
using LabelVoice.Core.Audio.SDLPlaybackImpl;
using SDL2;

// 部分参考 https://blog.csdn.net/chinaherolts2008/article/details/114304349

// 现有特性或问题
// 1. 修改缓冲区长度或修改采样源后,需要重新选择音频设备
// 2. 音频设备变更的检测只在播放期间进行(另一个线程轮循)
//    因此可能出现,当选择音频设备后,还未进行播放,但当前音频设备已被移除,这样播放时才会检测到并立即停止
// 3. 播放线程不会自动退出,因此在主线程即将退出如窗口关闭时,需要先停止播放(否则只能等音频放完)

// 可处理的事件
// 1. 音频设备变更
// 2. 播放状态变更

namespace LabelVoice.Core.Audio
{
    public class SDLPlayback : IAudioPlayback, IDisposable
    {
        // 构造函数
        public SDLPlayback()
        {
            // 初始化SDL引擎,有备无患
            _ = SDLHost.InitHost();

            // 创建私有结构
            _d = new SDLPlaybackData();

            // 转发事件
            _d.devChanged = (newVal, oldVal) =>
            {
                AudioDeviceChangedHandler?.Invoke(newVal, oldVal);
                // Console.WriteLine($"SDLPLayback: Audio device change to {newVal}.");
            };
            _d.stateChanged = (newVal, oldVal) =>
            {
                PlayStateChangedHandler?.Invoke(newVal, oldVal);
                // Console.WriteLine($"SDLPLayback: Play state change to {newVal}.");
            };
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _d.setDriver("directsound");
            }
        }

        // 初始化音频
        public void Init(ISampleProvider sampleProvider)
        {
            // 先关闭音频
            Stop();

            // 创建音频结构体
            _d.spec.freq = sampleProvider.WaveFormat.SampleRate;
            _d.spec.format = SDLGlobal.PLAYBACK_FORMAT;
            _d.spec.channels = (byte)sampleProvider.WaveFormat.Channels;
            _d.spec.silence = 0;

            _d.sampleProvider = sampleProvider;
            _d.setDevId(0);

            // 打开默认音频设备延迟到播放操作时
        }

        // 移除音频
        public void Dispose()
        {
            // 先关闭音频
            Stop();

            _d.sampleProvider = null;
            _d.setDevId(0);
            
            _d.setDriver("");
        }

        // 音频播放状态
        public PlaybackState GetPlaybackState()
        {
            return _d.state;
        }

        // 暂停
        public void Pause()
        {
            Stop();
        }

        // 播放
        public void Play()
        {
            if (_d.state == PlaybackState.Playing)
            {
                return;
            }

            if (!IsReady)
            {
                Console.WriteLine("SDLPlayback: Audio provider is not specified.");
                return;
            }

            // 如果没有打开音频设备那么打开第一个音频设备
            if (_d.curDevId == 0)
            {
                SwitchDevice(Guid.Empty, 0);
            }

            _d.start();
        }

        // 停止
        public void Stop()
        {
            if (_d.state == PlaybackState.Stopped)
            {
                return;
            }

            _d.stop();
        }

        // 音频设备数量
        public int DeviceNumber => SDLHost.Instance.NumOutputDevices();

        // 获取所有音频设备
        public List<AudioDevice> GetDevices()
        {
            var res = new List<AudioDevice>();
            int cnt = SDL.SDL_GetNumAudioDevices(0);
            for (int i = 0; i < cnt; i++)
            {
                var dev = new AudioDevice();
                dev.api = SDL.SDL_GetCurrentAudioDriver();
                dev.name = SDL.SDL_GetAudioDeviceName(i, 0);
                dev.deviceNumber = i;
                res.Add(dev);
            }

            return res;
        }

        public void SwitchDevice(Guid guid, int deviceNumber)
        {
            if (_d.state == PlaybackState.Playing)
            {
                Console.WriteLine("SDLPlayback: Don't change audio device when playing.");
                return;
            }

            _d.devGuid = guid;
            _d.devNum = deviceNumber;

            // 打开音频设备
            uint id;
            if ((id = SDL.SDL_OpenAudioDevice(
                    SDL.SDL_GetAudioDeviceName(_d.devNum, 0),
                    0,
                    ref _d.spec,
                    out _,
                    0)) == 0)
            {
                throw new IOException($"SDLPlayback: Failed to open audio device: {SDL.SDL_GetError()}.");
            }

            Console.WriteLine($"SDLPlayback: {SDL.SDL_GetAudioDeviceName(_d.devNum, 0)}");

            _d.setDevId(id);
        }

        public List<string> GetDrivers()
        {
            var res = new List<string>();
            int cnt = SDL.SDL_GetNumAudioDrivers();
            for (int i = 0; i < cnt; i++)
            {
                var dev = SDL.SDL_GetAudioDriver(i);
                if (dev == "dummy" || dev == "disk")
                {
                    continue;
                }
                res.Add(dev);
            }
            return res;
        }

        public void SwitchDriver(string driver)
        {
            _d.setDriver(driver);
        }

        private SDLPlaybackData _d;

        /**
         *
         * 其他方法
         * 
         */

        // 音频设备移除事件
        public SDLGlobal.ValueChangeEvent<uint> AudioDeviceChangedHandler;

        // 音频状态变更事件
        public SDLGlobal.ValueChangeEvent<PlaybackState> PlayStateChangedHandler;

        // 缓冲区采样数
        public ushort BufferSamples
        {
            get => _d.spec.samples;
            set
            {
                if (_d.state == PlaybackState.Playing)
                {
                    Console.WriteLine("SDLPlayback: Don't change this argument when playing.");
                    return;
                }

                _d.spec.samples = value;
                _d.setDevId(0);
            }
        }

        // 是否就绪
        public bool IsReady => _d.sampleProvider != null;
    }
}