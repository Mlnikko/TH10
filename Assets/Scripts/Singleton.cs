public class Singleton<T> where T : new()
{
    static readonly object locker = new();
    static T instance;
    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                lock (locker)
                {
                    instance ??= new T();
                }
            }
            return instance;
        }
    }
}
