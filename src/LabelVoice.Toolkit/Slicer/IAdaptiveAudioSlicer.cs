using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace LabelVoice.Toolkit.Slicer;

public interface IAdaptiveAudioSlicer : IAudioSlicer
{
    void Init(ISampleProvider provider);
}
