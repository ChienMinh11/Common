using UnityEngine;

namespace MyFramework
{
    public class IntConverter : INumberConverter<int>
    {
        public int Parse(string value) => int.Parse(value);
        public string ToString(int value) => value.ToString();
        public int Zero => 0;
        public int Add(int a, int b) => a + b;
        public int Subtract(int a, int b) => a - b;
        public bool IsLessThan(int a, int b) => a < b;
        public int MaxValue => int.MaxValue;
    }
}
