using System;
using ChieChie.Constracts;

namespace Game.GamePlay
{
   
    public class RealTimeProvider : ITimeProvider
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }

    public class VirtualTimeProvider : ITimeProvider
    {
        private TimeSpan _offset = TimeSpan.Zero;

        public DateTime UtcNow => DateTime.UtcNow.Add(_offset);

        /// <summary>
        /// Cộng thêm một khoảng thời gian giả lập (ví dụ: qua ngày mới, qua tháng mới)
        /// </summary>
        public void AddTime(TimeSpan duration)
        {
            _offset = _offset.Add(duration);
        }

        /// <summary>
        /// Đặt thời gian chính xác tới một mốc cụ thể nào đó để test
        /// </summary>
        public void SetAbsoluteTime(DateTime targetUtcTime)
        {
            _offset = targetUtcTime - DateTime.UtcNow;
        }

        /// <summary>
        /// Reset về thời gian thực của máy
        /// </summary>
        public void Reset()
        {
            _offset = TimeSpan.Zero;
        }
    }
}
