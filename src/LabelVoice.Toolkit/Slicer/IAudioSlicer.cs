using NAudio.Wave;

namespace LabelVoice.Toolkit.Slicer;

public interface IAudioSlicer
{
    public bool TrySlice(out AudioRange range);
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
