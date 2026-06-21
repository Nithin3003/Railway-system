namespace RailwayReservationSystem.Common
{
    /// <summary>
    /// Extension methods for string validation
    /// </summary>
    public static class StringExtensions
    {
        public static bool IsEmpty(this string? value)
        {
            return string.IsNullOrWhiteSpace(value);
        }

        public static bool IsNotEmpty(this string? value)
        {
            return !string.IsNullOrWhiteSpace(value);
        }

        public static string SafeTrim(this string? value)
        {
            return value?.Trim() ?? string.Empty;
        }

        public static string SafeToUpper(this string? value)
        {
            return value?.ToUpper() ?? string.Empty;
        }
    }
}
