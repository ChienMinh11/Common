using System;
using System.Text;
using UnityEngine;

namespace ChieChie.Core
{
    public static class TimeFormatter 
    {
        public static string FormatTime(float seconds)
        {
            if (seconds > 86400) 
            {
                return $"{seconds / 86400:0.#}d";
            }
            else if (seconds >= 3600) // Hours
            {
                return $"{seconds / 3600:0.#}h";
            }
            else if (seconds >= 60) // Minutes
            {
                return $"{seconds / 60:0.#}m";
            }
            else 
            {
                return $"{seconds:0}s";
            }
        }
        
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
