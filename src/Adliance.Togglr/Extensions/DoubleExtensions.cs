using System;

namespace Adliance.Togglr.Extensions
{
    public static class DoubleExtensions
    {
        public static string FormatColor(this double value, string? append = null, bool hideWhenZero = true)
        {
            value = Math.Round(value, 2);
            
            if (value > 0)
            {
                return $"<span class=\"has-text-success\">+{value:N2}{append}</span>";
            }
            if (value < 0)
            {
                return $"<span class=\"has-text-danger\">{value:N2}{append}</span>";
            }

            if (hideWhenZero)
            {
                return "";
            }

            return $"<span class=\"has-text-light\">{value:N2}{append}</span>";
        }
    }
}