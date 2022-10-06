using NAudio.Wave;

namespace LabelVoice.Toolkit.Slicer;

public interface ISupervisedAudioSlicer<in T> : IAudioSlicer
{
    public void Init(ISampleProvider provider, T supervision);
}
