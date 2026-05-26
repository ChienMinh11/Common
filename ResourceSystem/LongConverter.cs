using UnityEngine;

namespace MyFramework
{
    public class LongConverter : INumberConverter<long>
    {
        public long Parse(string value) => long.Parse(value);
        public string ToString(long value) => value.ToString();
        public long Zero => 0L;
        public long Add(long a, long b) => a + b;
        public long Subtract(long a, long b) => a - b;
        public bool IsLessThan(long a, long b) => a < b;
        public long MaxValue => long.MaxValue;
    }

}
