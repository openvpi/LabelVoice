namespace LabelVoice.Core.Utils
{
    public abstract class SingletonBase<T> where T : class
    {
        private static readonly Lazy<T> _instance = new(
            () => Activator.CreateInstance(typeof(T), true) as T);

        public static T Instance => _instance.Value;
    }
}