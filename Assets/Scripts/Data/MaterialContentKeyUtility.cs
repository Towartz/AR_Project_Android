namespace ARtiGraf.Data
{
    public static class MaterialContentKeyUtility
    {
        public static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return value.Trim()
                .ToLowerInvariant()
                .Replace(" ", string.Empty)
                .Replace("_", string.Empty)
                .Replace("-", string.Empty);
        }

        public static bool Matches(string left, string right)
        {
            return Normalize(left) == Normalize(right);
        }
    }
}
