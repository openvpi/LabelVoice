using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks.Dataflow;
using FFmpeg.AutoGen;
using NAudio.Wave;

namespace Test.Playback;

// 部分参考
// http://dranger.com/ffmpeg/tutorial07.html
// https://blog.csdn.net/edzjx/article/details/108645085

public unsafe class FFmpegWaveProvider : WaveStream, ISampleProvider
{
    public struct WaveArguments
    {
        public int SampleRate;
        public AVSampleFormat SampleFormat;
        public int Channels;

        public WaveArguments() : this(-1, AVSampleFormat.AV_SAMPLE_FMT_NONE, -1)
        {
        }

        public WaveArguments(AVSampleFormat fmt) : this(-1, fmt, -1)
        {
        }

        public WaveArguments(int sampleRate, AVSampleFormat fmt, int channels)
        {
            SampleRate = sampleRate;
            SampleFormat = fmt;
            Channels = channels;
        }

        public int BytesPerSample => ffmpeg.av_get_bytes_per_sample(SampleFormat);
    }

    public FFmpegWaveProvider(string fileName) : this(fileName, new WaveArguments())
    {
    }

    public FFmpegWaveProvider(string fileName, WaveArguments args)
    {
        _fileName = fileName;
        _arguments = args;

        // 初始化互斥量
        lockObject = new object();

        initDecoder();
    }

    ~FFmpegWaveProvider()
    {
        lock (lockObject)
        {
            quitDecoder();
        }
    }

    public string FileName => _fileName;
    public WaveArguments Arguments => _arguments;

    public WaveFormat OriginalWaveFormat => _waveFormat;

    public override WaveFormat WaveFormat => _resampledFormat;

    // This value is estimated from bitrate, may be inaccurate
    public override long Length => src2dest_bytes(_length) * _arguments.Channels;

    // This value is estimated from bitrate, may be inaccurate
    public override long Position
    {
        get => src2dest_bytes(_pos) * _arguments.Channels;
        set
        {
            lock (lockObject)
            {
                _pos = dest2src_bytes(value / _arguments.Channels);
                seek();
            }
        }
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        // var waveBuffer = new WaveBuffer(buffer);
        // int samplesRequired = count / 4;
        // int samplesRead = Read(waveBuffer.FloatBuffer, offset / 4, samplesRequired);
        // return samplesRead * 4;

        int res;
        lock (lockObject)
        {
            if (offset > 0)
            {
                _ = decode(IntPtr.Zero, offset);
            }

            var gch = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            res = decode(Marshal.UnsafeAddrOfPinnedArrayElement(buffer, 0), count);
            gch.Free();
        }

        return res;
    }

    public int Read(float[] buffer, int offset, int count)
    {
        if (Arguments.SampleFormat != AVSampleFormat.AV_SAMPLE_FMT_FLT)
        {
            throw new NotImplementedException("FFmpeg: Set the sample format to FLT.");
        }

        int res;
        lock (lockObject)
        {
            int bytesPerSample = 4;
            offset *= bytesPerSample;
            count *= bytesPerSample;
            if (offset > 0)
            {
                _ = decode(IntPtr.Zero, offset);
            }

            var gch = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            res = decode(Marshal.UnsafeAddrOfPinnedArrayElement(buffer, 0), count);
            gch.Free();

            res /= bytesPerSample;
        }

        return res;
    }

    public float Volume => throw new NotImplementedException();

    /*
     *
     * Private part
     * 
     */

    // FFmpeg 指针
    private AVFormatContext* _formatContext;

    private AVCodecContext* _codecContext;

    private SwrContext* _swrContext;

    private AVPacket* _packet;

    private AVFrame* _frame;

    // 标准音频信息
    private WaveFormat _waveFormat;

    private WaveFormat _resampledFormat;

    private string _fileName;

    private WaveArguments _arguments;

    private long _length; // 不包括声道

    private long _pos; // 不包括声道

    private int _audioIndex; // 音频流序号

    private AVChannelLayout _channelLayout; // 输出声道布局

    private LinkedList<byte> _cachedBuffer;

    private Object lockObject;

    private void initDecoder()
    {
        var fmt_ctx = ffmpeg.avformat_alloc_context();

        // 打开文件
        var ret = ffmpeg.avformat_open_input(&fmt_ctx, _fileName, null, null);
        if (ret != 0)
        {
            ffmpeg.avformat_free_context(fmt_ctx);

            throw new FileLoadException($"FFmpeg: Failed to load file {_fileName}.");
        }

        _formatContext = fmt_ctx;

        // 查找流信息
        ret = ffmpeg.avformat_find_stream_info(fmt_ctx, null);
        if (ret < 0)
        {
            throw new DecoderFallbackException("FFmpeg: Failed to find streams.");
        }

        // 查找音频流
        var audio_idx = ffmpeg.av_find_best_stream(fmt_ctx, AVMediaType.AVMEDIA_TYPE_AUDIO,
            -1, -1, null, 0);
        if (audio_idx < 0)
        {
            throw new DecoderFallbackException("FFmpeg: Failed to find audio stream.");
        }

        _audioIndex = audio_idx;

        // 查找解码器
        var stream = fmt_ctx->streams[audio_idx];
        var codec_param = stream->codecpar;
        var codec = ffmpeg.avcodec_find_decoder(codec_param->codec_id);
        if (codec == null)
        {
            throw new DecoderFallbackException("FFmpeg: Failed to find decoder.");
        }

        // 分配解码器上下文
        var codec_ctx = ffmpeg.avcodec_alloc_context3(null);
        _codecContext = codec_ctx;

        // 传递解码器信息
        ret = ffmpeg.avcodec_parameters_to_context(codec_ctx, codec_param);
        if (ret < 0)
        {
            throw new DecoderFallbackException("FFmpeg: Failed to pass params to context.");
        }

        // 打开解码器
        ret = ffmpeg.avcodec_open2(codec_ctx, codec, null);
        if (ret < 0)
        {
            throw new DecoderFallbackException("FFmpeg: Failed to open decoder.");
        }

        int srcBytesPerSample = ffmpeg.av_get_bytes_per_sample(codec_ctx->sample_fmt);
        int srcChannels = codec_ctx->ch_layout.nb_channels;

        // 记录音频信息
        _length = (long)
            (stream->duration * (stream->time_base.num / (float)stream->time_base.den)
                              * codec_ctx->sample_rate * srcBytesPerSample);

        _waveFormat = new WaveFormat(
            codec_ctx->sample_rate,
            srcBytesPerSample * 8,
            srcChannels
        );

        // 修改缺省参数
        if (_arguments.SampleRate < 0)
        {
            _arguments.SampleRate = codec_ctx->sample_rate;
            Console.WriteLine($"FFmpeg: Set default sample rate as {_arguments.SampleRate}");
        }

        if (_arguments.SampleFormat < 0)
        {
            _arguments.SampleFormat = codec_ctx->sample_fmt;
            Console.WriteLine(
                $"FFmpeg: Set default sample format as {ffmpeg.av_get_sample_fmt_name(_arguments.SampleFormat)}");
        }

        if (_arguments.Channels < 0)
        {
            _arguments.Channels = srcChannels > 2 ? 2 : srcChannels;
            Console.WriteLine($"FFmpeg: Set default channels as {_arguments.Channels}");
        }

        if (_arguments.Channels > 2)
        {
            throw new NotImplementedException("FFmpeg: Only support basic mono and stereo.");
        }

        // 对外表现的格式
        if (_arguments.SampleFormat == AVSampleFormat.AV_SAMPLE_FMT_FLT)
        {
            _resampledFormat = WaveFormat.CreateIeeeFloatWaveFormat(_arguments.SampleRate, _arguments.Channels);
        }
        else
        {
            _resampledFormat =
                new WaveFormat(_arguments.SampleRate, _arguments.BytesPerSample * 8, _arguments.Channels);
        }

        AVChannelLayout in_ch_layout;
        AVChannelLayout out_ch_layout;
        ffmpeg.av_channel_layout_copy(&in_ch_layout, &codec_ctx->ch_layout);
        ffmpeg.av_channel_layout_default(&out_ch_layout, _arguments.Channels);
        fixed (AVChannelLayout* ch_layout_ptr = &_channelLayout)
        {
            ffmpeg.av_channel_layout_copy(ch_layout_ptr, &out_ch_layout);
        }

        // 初始化重采样器
        var swr = ffmpeg.swr_alloc();
        _swrContext = swr;

        ret = ffmpeg.swr_alloc_set_opts2(&swr, &out_ch_layout, _arguments.SampleFormat,
            _arguments.SampleRate, &in_ch_layout,
            codec_ctx->sample_fmt, codec_ctx->sample_rate, 0, null);
        if (ret != 0)
        {
            throw new DecoderFallbackException("FFmpeg: Failed to create resampler.");
        }

        ret = ffmpeg.swr_init(swr);
        if (ret < 0)
        {
            throw new DecoderFallbackException("FFmpeg: Failed to init resampler.");
        }

        // 初始化数据包和数据帧
        var pkt = ffmpeg.av_packet_alloc();
        var frame = ffmpeg.av_frame_alloc();

        // 等待进一步的解码
        _packet = pkt;
        _frame = frame;

        ffmpeg.av_channel_layout_uninit(&in_ch_layout);
        ffmpeg.av_channel_layout_uninit(&out_ch_layout);
    }

    private int decode(IntPtr buf, int size)
    {
        var fmt_ctx = _formatContext;
        var codec_ctx = _codecContext;
        var swr_ctx = _swrContext;

        var pkt = _packet;
        var frame = _frame;

        var stream = fmt_ctx->streams[_audioIndex];

        if (_cachedBuffer == null)
        {
            _cachedBuffer = new LinkedList<byte>();
        }

        // 准备声道信息
        AVChannelLayout out_ch_layout;
        fixed (AVChannelLayout* ch_layout_ptr = &_channelLayout)
        {
            ffmpeg.av_channel_layout_copy(&out_ch_layout, ch_layout_ptr);
        }

        while (_cachedBuffer.Count < size)
        {
            int ret = ffmpeg.av_read_frame(fmt_ctx, pkt);

            // 判断是否结束
            if (ret != 0)
            {
                _pos = _length;

                ffmpeg.av_packet_unref(pkt);
                break;
            }

            // 跳过其他流
            if (pkt->stream_index != _audioIndex)
            {
                ffmpeg.av_packet_unref(pkt);
                continue;
            }

            // 发送待解码包
            ret = ffmpeg.avcodec_send_packet(codec_ctx, pkt);
            ffmpeg.av_packet_unref(pkt);
            if (ret < 0)
            {
                // 忽略
                Console.WriteLine("FFmpeg: Error submitting a packet for decoding with code {0:x}, ignored.", -ret);
            }

            while (ret >= 0)
            {
                // 接收解码数据
                ret = ffmpeg.avcodec_receive_frame(codec_ctx, frame);
                if (ret == ffmpeg.AVERROR_EOF || ret == ffmpeg.AVERROR(ffmpeg.EAGAIN))
                {
                    // 结束
                    break;
                }
                else if (ret < 0)
                {
                    // 出错
                    ffmpeg.av_frame_unref(frame);

                    // 忽略
                    Console.WriteLine("FFmpeg: Error decoding frame with code {0:x}, ignored.", -ret);
                }

                // 记录当前时间
                _pos = (long)(frame->best_effort_timestamp / (float)stream->duration * _length);
                Console.WriteLine(frame->best_effort_timestamp);

                // 进行重采样
                var resampled_frame = ffmpeg.av_frame_alloc();
                resampled_frame->sample_rate = _arguments.SampleRate;
                resampled_frame->format = (int)_arguments.SampleFormat;
                ffmpeg.av_channel_layout_copy(&resampled_frame->ch_layout, &out_ch_layout);

                ret = ffmpeg.swr_convert_frame(swr_ctx, resampled_frame, frame);

                if (ret == 0)
                {
                    int sz = ffmpeg.av_samples_get_buffer_size(null, resampled_frame->ch_layout.nb_channels,
                        resampled_frame->nb_samples, _arguments.SampleFormat, 1);
                    var arr = resampled_frame->data[0];
                    for (int i = 0; i < sz; ++i)
                    {
                        _cachedBuffer.AddLast(arr[i]);
                    }
                }
                else
                {
                    // 忽略
                    if (ret == ffmpeg.AVERROR_INVALIDDATA)
                    {
                        Console.WriteLine("FFmpeg: Error resampling frame with code {0:x}, ignored.", -ret);
                    }
                    else
                    {
                        throw new DecoderFallbackException(
                            String.Format("FFmpeg: Error resampling frame with code {0:x}.", -ret));
                    }
                }

                ffmpeg.av_frame_free(&resampled_frame);
                ffmpeg.av_frame_unref(frame);
            }
        }

        int cnt = 0;
        while (_cachedBuffer.First != null && cnt < size)
        {
            if (buf != IntPtr.Zero)
            {
                *((byte*)buf + cnt) = _cachedBuffer.First.Value;
            }

            _cachedBuffer.RemoveFirst();
            cnt++;
        }

        ffmpeg.av_channel_layout_uninit(&out_ch_layout);
        return cnt;
    }

    // 参数pos表示输入参数下包括声道的字节偏移
    private void seek()
    {
        var fmt_ctx = _formatContext;
        var swr_ctx = _swrContext;
        var codec_ctx = _codecContext;

        // 清空重采样缓冲区
        // 杂音问题：https://blog.csdn.net/wangyequn1124/article/details/104309632
        // 示例代码：https://blog.csdn.net/fengbingchun/article/details/90313604
        // 他人代码：https://blog.csdn.net/wanggao_1990/article/details/115731502
        // {
        //     var delay = ffmpeg.swr_get_delay(swr_ctx, _waveFormat.SampleRate);
        //     if (delay > 0)
        //     {
        //         int src_nb_samples = 44100;
        //         int dst_nb_samples = (int)ffmpeg.av_rescale_rnd(
        //             delay + src_nb_samples,
        //             _arguments.SampleRate,
        //             _waveFormat.SampleRate, AVRounding.AV_ROUND_UP);
        //
        //         Console.WriteLine($"Dst samples {dst_nb_samples}");
        //
        //         byte** dst_data;
        //         int dst_linesize;
        //         byte** src_data;
        //         int src_linesize;
        //
        //         double t = 0;
        //         int ret = 0;
        //
        //         // 分配空的源
        //         ret = ffmpeg.av_samples_alloc_array_and_samples(&src_data, &src_linesize, _waveFormat.Channels,
        //             src_nb_samples, codec_ctx->sample_fmt, 0);
        //         if (ret < 0)
        //         {
        //             throw new OutOfMemoryException("FFmpeg: Fail to av_samples_alloc_array_and_samples.");
        //         }
        //
        //         // // 分配目标
        //         ret = ffmpeg.av_samples_alloc_array_and_samples(&dst_data, &dst_linesize, _arguments.Channels,
        //             dst_nb_samples, _arguments.SampleFormat, 0);
        //         if (ret < 0)
        //         {
        //             throw new OutOfMemoryException("FFmpeg: Fail to av_samples_alloc_array_and_samples.");
        //         }
        //
        //         // 转换
        //         // fill_samples((double*)src_data[0], src_nb_samples, _waveFormat.Channels, _waveFormat.SampleRate, &t);
        //         ret = ffmpeg.swr_convert(swr_ctx, dst_data, dst_nb_samples, null, 0);
        //         if (ret < 0)
        //         {
        //             Console.WriteLine("FFmpeg: Fail to swr_convert with code {0:x}, ignored.", -ret);
        //         }
        //         else
        //         {
        //             Console.WriteLine($"Flush {ret}");
        //         }
        //
        //         ffmpeg.av_freep(&dst_data[0]);
        //         ffmpeg.av_freep(&dst_data);
        //
        //         ffmpeg.av_freep(&src_data[0]);
        //         ffmpeg.av_freep(&src_data);
        //     }
        // }
        Console.WriteLine($"seek {_pos}");
        if (_cachedBuffer != null && _cachedBuffer.First != null)
        {
            _cachedBuffer.Clear();
        }

        var stream = fmt_ctx->streams[_audioIndex];
        var timestamp = (long)(_pos / (float)_length * stream->duration);
        ffmpeg.av_seek_frame(fmt_ctx, _audioIndex, timestamp, ffmpeg.AVSEEK_FLAG_FRAME);
    }

    private void quitDecoder()
    {
        var fmt_ctx = _formatContext;
        var codec_ctx = _codecContext;
        var swr_ctx = _swrContext;

        var pkt = _packet;
        var frame = _frame;

        if (frame != null)
        {
            ffmpeg.av_frame_free(&frame);
        }

        if (pkt != null)
        {
            ffmpeg.av_packet_free(&pkt);
        }

        if (swr_ctx != null)
        {
            ffmpeg.swr_close(swr_ctx);
            ffmpeg.swr_free(&swr_ctx);
        }

        if (codec_ctx != null)
        {
            ffmpeg.avcodec_close(codec_ctx);
            ffmpeg.avcodec_free_context(&codec_ctx);
        }

        if (fmt_ctx != null)
        {
            ffmpeg.avformat_close_input(&fmt_ctx);
        }

        _frame = null;
        _packet = null;
        _swrContext = null;
        _codecContext = null;
        _formatContext = null;

        fixed (AVChannelLayout* ch_layout_ptr = &_channelLayout)
        {
            ffmpeg.av_channel_layout_uninit(ch_layout_ptr);
        }
    }

    public static void RegisterLibraries(string dir)
    {
        ffmpeg.RootPath = dir;
    }

    private long src2dest_bytes(long bytes)
    {
        return (long)(bytes / (_waveFormat.BitsPerSample / (float)8) / _waveFormat.SampleRate *
                      _arguments.SampleRate * _arguments.BytesPerSample);
    }

    private long dest2src_bytes(long bytes)
    {
        return (long)(bytes / (float)_arguments.BytesPerSample / _arguments.SampleRate *
                      _waveFormat.SampleRate * (_waveFormat.BitsPerSample / (float)8));
    }

    private static void fill_samples(double* dst, int nb_samples, int nb_channels, int sample_rate, double* t)
    {
        Console.WriteLine("Fill Samples Begin");

        double tincr = 1.0 / sample_rate;
        double* dstp = dst;
        const double c = 2 * ffmpeg.M_PI * 440.0;

        // generate sin tone with 440Hz frequency and duplicated channels
        for (int i = 0; i < nb_samples; i++)
        {
            *dstp = Math.Sin(c * *t);
            for (int j = 1; j < nb_channels; j++)
                dstp[j] = dstp[0];

            dstp += nb_channels;
            *t += tincr;
        }

        Console.WriteLine("Fill Samples End");
    }
}