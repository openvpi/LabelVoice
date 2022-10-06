using NAudio.Wave;

namespace LabelVoice.Toolkit.Slicer;

/// <summary>
/// An adaptive audio slicer slices audios with no knowledge or supervision other than the signal itself.
/// </summary>
public interface IAdaptiveAudioSlicer : IAudioSlicer
{
    /// <summary>
    /// Initialize the audio slicer with signal provided by <paramref name="provider"/>.
    /// </summary>
    void Init(ISampleProvider provider);
}
