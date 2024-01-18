﻿using System.Text.RegularExpressions;

namespace EngineAPI.Service
{
    public static class Functions
    {
        public static string EscapeString(string value)
        {
            // Implement your logic to escape special characters if needed
            return value.Replace("\"", "\\\"");
        }

        public static string CleanUpString(string value)
        {
            string cleanedString = Regex.Replace(value, @"\^\^.*$", "");
            return cleanedString;
        }
    }
 
}
