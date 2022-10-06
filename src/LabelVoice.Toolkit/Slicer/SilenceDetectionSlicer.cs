using NAudio.Wave;

namespace LabelVoice.Toolkit.Slicer;

public class SilenceDetectionSlicer : MonoAudioSlicerBase, IAdaptiveAudioSlicer
{
    public void Init(ISampleProvider provider)
    {
        Provider = provider;
    }

    public override bool TrySlice(out AudioRange range)
    {
        throw new NotImplementedException();
    }
}
