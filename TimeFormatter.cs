using System;
using System.Text;

namespace ChieChie.Resource
{
    public static class TimeFormatter 
    {
        
        private static readonly StringBuilder StringBuilder = new StringBuilder();
        

        public static string FormatRemainingTime(TimeSpan timeSpan)
        {
            if (timeSpan <= TimeSpan.Zero) return "00:00";

            StringBuilder.Clear();
    
            if (timeSpan.TotalDays >= 1)
            {
                StringBuilder.Append((int)timeSpan.TotalDays).Append("d ")
                    .Append(timeSpan.Hours.ToString("D2")).Append(":")
                    .Append(timeSpan.Minutes.ToString("D2")).Append(":")
                    .Append(timeSpan.Seconds.ToString("D2"));
            }
            else if (timeSpan.TotalHours >= 1)
            {
                StringBuilder.Append(timeSpan.Hours.ToString("D2")).Append(":")
                    .Append(timeSpan.Minutes.ToString("D2")).Append(":")
                    .Append(timeSpan.Seconds.ToString("D2"));
            }
        
            else
            {
                StringBuilder.Append(timeSpan.Minutes.ToString("D2")).Append(":")
                    .Append(timeSpan.Seconds.ToString("D2"));
            }

            return StringBuilder.ToString();
        }
    }
}
