namespace Watermark.Core;

public static class Utils
{
    public static bool TryParsePercentOrNumber(string value, float basis, out float result)
    {
        result = 0;
        if (string.IsNullOrWhiteSpace(value)) return false;
        if (value.Trim().EndsWith("%"))
        {
            if (float.TryParse(value.Trim().TrimEnd('%'), out var p))
            {
                result = basis * (p / 100f);
                return true;
            }
            return false;
        }
        return float.TryParse(value, out result);
    }
}
