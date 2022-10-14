// See https://aka.ms/new-console-template for more information

using FFmpeg.AutoGen;
using LabelVoice.Core.Audio;
using LabelVoice.Core.Audio.SDLPlaybackImpl;
using NAudio.Wave;

namespace Test.Playback
{
    public static class Program
    {
        static int Main(String[] args)
        {
            return Fun2(args);
        }

        static int Fun2(String[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: Test.Playback <audio file>\n");
                return 0;
            }

            // 输出版本
            var ver = SDLHost.Instance.GetVersion();
            Console.WriteLine($"SDL: {ver[0]},{ver[1]},{ver[2]}");

            FFmpegWaveProvider.RegisterLibraries("C:\\Users\\truef\\Documents\\GitHub\\LabelVoice\\bin");

            // 读取音频
            var file = new FFmpegWaveProvider(args[0],
                new FFmpegWaveProvider.WaveArguments(44100, AVSampleFormat.AV_SAMPLE_FMT_FLT, 2));
            // var file = new AudioFileReader(args[0]);
            var playback = new SDLPlayback();

            var devs = playback.GetDevices();
            for (int i = 0; i < devs.Count; ++i)
            {
                Console.WriteLine($"{i}: {devs[i].name}");
            }

            playback.Init(file);

            // 选择音频设备
            int idx;
            do
            {
                idx = int.Parse(Console.ReadLine());
            } while (idx >= devs.Count || idx < 0);

            playback.SwitchDevice(Guid.Empty, idx);
            playback.Play();

            // 等待键盘中断
            Console.ReadLine();

            playback.Stop();

            return 0;
        }
    }
}