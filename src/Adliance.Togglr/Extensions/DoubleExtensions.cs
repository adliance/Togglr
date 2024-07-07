using System;
using System.Globalization;

namespace Adliance.Togglr.Extensions;

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

    public static string HideWhenZero(this double value, bool formatWithCommas = true)
    {
        if (value == 0) return "";

        if (formatWithCommas) return value.ToString("N2", CultureInfo.CurrentCulture);
        return value.ToString("N0", CultureInfo.CurrentCulture);
    }

    public static string HideWhenZero(this int value)
    {
        if (value == 0) return "";
        return value.ToString("N0", CultureInfo.CurrentCulture);
    }

    public static string FormatBillable(this double value)
    {
        if (double.IsNaN(value)) return "";

        value = Math.Round(value, 2);

        if (value > 90)
        {
            return $"<span class=\"has-text-success\">{value:N0}</span>";
        }

        if (value < 80)
        {
            return $"<span class=\"has-text-danger\">{value:N0}</span>";
        }

        return $"<span>{value:N0}</span>";
    }
}
