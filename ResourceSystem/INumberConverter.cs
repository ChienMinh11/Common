namespace GameCore.Runtime
{
    public interface INumberConverter<T>
    {
        T Parse(string value);
        string ToString(T value);
        T Zero { get; }
        T Add(T a, T b);
        T Subtract(T a, T b);
        bool IsLessThan(T a, T b);
        T MaxValue { get; }
    }
}
