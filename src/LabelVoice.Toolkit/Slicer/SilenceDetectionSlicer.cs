using NAudio.Wave;

namespace LabelVoice.Toolkit.Slicer;

/// <summary>
/// A silence detection slicer slices audios via silence detection, i.e. cuts off audios from detected silence parts.
/// </summary>
public class SilenceDetectionSlicer : MonoAudioSlicerBase, IAdaptiveAudioSlicer
{
    public void Init(ISampleProvider provider)
    {
        Provider = provider;
    }

    public override bool TrySlice(out AudioRange? range)
    {
        throw new NotImplementedException();
    }
}
