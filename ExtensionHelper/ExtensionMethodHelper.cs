using System;
using System.Collections.Generic;
using System.Text;

namespace WebApp.ExtensionMethods.Helpers
{
    public static class ExtensionMethodManager
    {
        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) 
                return value;

            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

        public static string RemoveTrailingChars(this string value)
        {
            char[] charsToTrim = { '0' };

            return value.TrimStart(charsToTrim);
        }

        public static string PadLeftAndTruncate(this string value, char padValue, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) 
                return value;

            var returnString = value.PadLeft(maxLength, padValue).Truncate(maxLength);

            return returnString;
        }

        public static string PadRightAndTruncate(this string value, char padValue, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) 
                return value;

            var returnString = value.PadRight(maxLength, padValue).Truncate(maxLength);

            return returnString;
        }

        public static string FormatStoreCode (this string value)
        {
            if (string.IsNullOrEmpty(value)) 
                return value;

            string formattedStoreCode = value.Replace(" ", "");

            if (formattedStoreCode.Length > 3)
            {
                if (formattedStoreCode.StartsWith("0"))
                {
                    return formattedStoreCode.Substring(1, 4);
                }

                return formattedStoreCode.Substring(0, 4);
            }
            else
            {
                return formattedStoreCode.PadLeft(4, '0');
            }
        }
    }
}
