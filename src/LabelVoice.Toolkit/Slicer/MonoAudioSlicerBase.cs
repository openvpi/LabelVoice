using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace LabelVoice.Toolkit.Slicer;

public abstract class MonoAudioSlicerBase : IAudioSlicer
{
    private ISampleProvider _provider;

    protected ISampleProvider Provider
    {
        get => _provider;
        set => _provider = RequireMono(value);
    }

    private static ISampleProvider RequireMono(ISampleProvider provider)
    {
        return provider.WaveFormat.Channels >= 2
            ? new StereoToMonoSampleProvider(provider)
            : provider;
    }

    public abstract bool TrySlice(out AudioRange range);
}
