namespace ChieChie.Core
{
    public class LongConverter : INumberConverter<long>
    {
        public long Parse(string value) => long.Parse(value);
        public string ToString(long value) => value.ToString();
        
        // Tối ưu hóa: Không sinh rác, trả về trực tiếp giá trị gốc
        public long ToLong(long value) => value;
        public long FromLong(long value) => value;
        public double ToDouble(long value) => value;

        public long Zero => 0L;
        public long Add(long a, long b) => a + b;
        public long Subtract(long a, long b) => a - b;
        public bool IsLessThan(long a, long b) => a < b;
        public long MaxValue => long.MaxValue;
    }

}
