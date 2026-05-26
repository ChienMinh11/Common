using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyFramework
{
    public static class NumberFormatter
    {
        private static readonly string[] Suffixes = 
        { 
            "", "K", "M", "B", "T", "Qa", "Qi", "Sx", "Sp", "Oc", "No", "Dc"
        };

        private static readonly Dictionary<string, long> SuffixValues = new()
        {
            {"K", 1_000},
            {"M", 1_000_000},
            {"B", 1_000_000_000},
            {"T", 1_000_000_000_000},
            {"Qa", 1_000_000_000_000_000},
            {"Qi", 1_000_000_000_000_000_000}
        };

        private const long MIN_SUFFIX_VALUE = 1_000_000; // 1 triệu

        public static string FormatNumber(long number)
        {
            if (number == 0) return "0";
            
            // Xử lý số âm
            if (number < 0)
            {
                return "-" + FormatNumber(-number);
            }

            // Nếu số nhỏ hơn 1 triệu, chỉ trả về số không có suffix
            if (number < MIN_SUFFIX_VALUE)
            {
                return number.ToString("#,##0");
            }

            // Tìm bậc của số (1000^n)
            int magnitude = 0;
            decimal decimalNumber = number;
            
            while (Math.Abs(decimalNumber) >= 1000 && magnitude < Suffixes.Length - 1)
            {
                decimalNumber /= 1000;
                magnitude++;
            }

            // Format số với độ chính xác phù hợp
            string result;
            if (decimalNumber >= 100) // 100-999
            {
                result = decimalNumber.ToString("0");
            }
            else if (decimalNumber >= 10) // 10-99
            {
                result = decimalNumber.ToString("0.0");
            }
            else // 0-9
            {
                result = decimalNumber.ToString("0.00");
            }

            // Loại bỏ số 0 thừa ở phần thập phân
            if (result.Contains("."))
            {
                result = result.TrimEnd('0').TrimEnd('.');
            }

            return result + Suffixes[magnitude];
        }

        public static long ParseFormattedNumber(string formattedNumber)
        {
            if (string.IsNullOrEmpty(formattedNumber))
                return 0;

            // Xử lý số âm
            bool isNegative = formattedNumber.StartsWith("-");
            if (isNegative)
            {
                formattedNumber = formattedNumber.Substring(1);
            }

            // Nếu số không có suffix, thử parse trực tiếp
            if (!HasSuffix(formattedNumber))
            {
                formattedNumber = formattedNumber.Replace(",", ""); // Xóa dấu phẩy ngăn cách
                if (long.TryParse(formattedNumber, out long result))
                {
                    return isNegative ? -result : result;
                }
                return 0;
            }

            // Tìm suffix trong chuỗi
            string suffix = "";
            string numberPart = formattedNumber;
            
            foreach (var kvp in SuffixValues)
            {
                if (formattedNumber.EndsWith(kvp.Key, StringComparison.OrdinalIgnoreCase))
                {
                    suffix = kvp.Key;
                    numberPart = formattedNumber.Substring(0, formattedNumber.Length - suffix.Length);
                    break;
                }
            }

            // Parse phần số
            if (!decimal.TryParse(numberPart, out decimal number))
                return 0;

            // Áp dụng hệ số theo suffix
            long finalValue = 0;
            if (string.IsNullOrEmpty(suffix))
            {
                finalValue = (long)number;
            }
            else if (SuffixValues.TryGetValue(suffix, out long multiplier))
            {
                finalValue = (long)(number * multiplier);
            }

            return isNegative ? -finalValue : finalValue;
        }

        private static bool HasSuffix(string number)
        {
            foreach (var suffix in SuffixValues.Keys)
            {
                if (number.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        public static string FormatNumberWithPrecision(long number, int minDecimals = 0, int maxDecimals = 2)
        {
            if (number == 0) return "0";

            if (number < 0)
            {
                return "-" + FormatNumberWithPrecision(-number, minDecimals, maxDecimals);
            }

            // Nếu số nhỏ hơn 1 triệu, trả về định dạng có dấu phẩy
            if (number < MIN_SUFFIX_VALUE)
            {
                return number.ToString("#,##0");
            }

            int magnitude = 0;
            decimal decimalNumber = number;

            while (Math.Abs(decimalNumber) >= 1000 && magnitude < Suffixes.Length - 1)
            {
                decimalNumber /= 1000;
                magnitude++;
            }

            string formatString = "0";
            if (maxDecimals > 0)
            {
                formatString += "." + new string('#', maxDecimals);
            }

            string result = decimalNumber.ToString(formatString);

            // Đảm bảo số lượng số thập phân tối thiểu
            if (minDecimals > 0)
            {
                int currentDecimals = result.Contains(".") ? result.Length - result.IndexOf('.') - 1 : 0;
                if (currentDecimals < minDecimals)
                {
                    if (!result.Contains("."))
                    {
                        result += ".";
                    }
                    result += new string('0', minDecimals - currentDecimals);
                }
            }

            return result + Suffixes[magnitude];
        }
    }
}