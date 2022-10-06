using NAudio.Wave;

namespace LabelVoice.Toolkit.Slicer;

/// <summary>
/// A supervised audio slicer slices audios with given knowledge or supervision such as transcriptions or subtitles.
/// </summary>
/// <typeparam name="T">The type of the instance carrying the supervision.</typeparam>
public interface ISupervisedAudioSlicer<in T> : IAudioSlicer
{
    /// <summary>
    /// Initialize the audio slicer with signal provided by <paramref name="provider"/> and knowledge carried by <paramref name="supervision"/>.
    /// </summary>
    /// <param name="provider"></param>
    /// <param name="supervision"></param>
    public void Init(ISampleProvider provider, T supervision);
}
