using NAudio.Wave;

namespace LabelVoice.Toolkit.Slicer;

/// <summary>
/// An audio slicer slices audios into multiple pieces in a streaming way.
/// </summary>
public interface IAudioSlicer
{
    /// <summary>
    /// Try to get the next piece.
    /// </summary>
    /// <param name="range">When this method returns, contains the range of next piece in the given signal, or <code>null</code> if there are no more pieces to be sliced.</param>
    /// <returns><see langword="true"/> if the next valid piece is sliced, otherwise <see langword="false"/>.</returns>
    public bool TrySlice(out AudioRange? range);
}

public readonly struct AudioRange
{
    public readonly double In;
    public readonly double Out;

    public AudioRange(double inPoint, double outPoint)
    {
        In = inPoint;
        Out = outPoint;
    }
}
