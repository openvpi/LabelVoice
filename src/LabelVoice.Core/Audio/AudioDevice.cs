namespace LabelVoice.Core.Audio
{
    public class AudioDevice
    {
        #region Fields

        public string name;

        public string api;

        public int deviceNumber;

        public Guid guid;

        public object data;

        #endregion Fields

        #region Methods

        public override string ToString() => $"[{api}] {name}";

        #endregion Methods
    }
}