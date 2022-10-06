using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace LabelVoice.Toolkit.Slicer;

/// <summary>
/// This abstract class represents audio slicers that slices mono audio signals and provides property and conversion to ensure the given signal is mono.
/// Any class that inherits this class gets the `Provider` property whose setter converts stereo providers into mono providers. Thus, the provider got from this property is guaranteed to be mono.<br/>
/// Example:
/// <code>
/// public void Init(ISampleProvider provider)
/// {
///     Provider = provider;
///     // Once assigned, the `Provider` property is guaranteed to provide mono signals.
/// }
/// </code>
/// </summary>
public abstract class MonoAudioSlicerBase : IAudioSlicer
{
    private ISampleProvider _provider;

    /// <summary>
    /// Represents the source provider which is guaranteed to provide mono signals.
    /// </summary>
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

    public abstract bool TrySlice(out AudioRange? range);
}
