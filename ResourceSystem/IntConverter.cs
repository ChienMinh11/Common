
namespace ChieChie.Core
{
    public class IntConverter : INumberConverter<int>
    {
        public int Parse(string value) => int.Parse(value);
        public string ToString(int value) => value.ToString();
        
        // Tối ưu hóa: Không boxing, không sinh chuỗi rác string
        public long ToLong(int value) => value;
        public int FromLong(long value) => (int)value;
        public double ToDouble(int value) => value;

        public int Zero => 0;
        public int Add(int a, int b) => a + b;
        public int Subtract(int a, int b) => a - b;
        public bool IsLessThan(int a, int b) => a < b;
        public int MaxValue => int.MaxValue;
    }
}
