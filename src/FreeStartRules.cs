namespace XicesBetterExperience
{
    internal static class FreeStartRules
    {
        internal const int Minimum = 0, Maximum = 10000, Default = 600;
        internal static int Clamp(int value) { return value < Minimum ? Minimum : value > Maximum ? Maximum : value; }
        internal static int ParseRange(string text, int previous, int minimum, int maximum)
        {
            if (string.IsNullOrWhiteSpace(text)) return previous;
            text = text.Trim();
            bool negative = text[0] == '-';
            int start = negative || text[0] == '+' ? 1 : 0;
            if (start == text.Length) return previous;
            int value = 0;
            for (int i = start; i < text.Length; i++)
            {
                if (text[i] < '0' || text[i] > '9') return previous;
                value = System.Math.Min(maximum, value * 10 + text[i] - '0');
            }
            return System.Math.Max(minimum, negative ? minimum : value);
        }
        internal static int ParseReputation(string text, int previous)
        {
            if (string.IsNullOrWhiteSpace(text)) return previous;
            text = text.Trim();
            bool negative = text[0] == '-';
            int start = negative || text[0] == '+' ? 1 : 0;
            if (start == text.Length) return previous;
            int value = 0;
            for (int i = start; i < text.Length; i++)
            {
                if (text[i] < '0' || text[i] > '9') return previous;
                value = System.Math.Min(200, value * 10 + text[i] - '0');
            }
            return negative ? -value : value;
        }
        internal static int Parse(string text, int previous)
        {
            if (string.IsNullOrWhiteSpace(text)) return Clamp(previous);
            text = text.Trim();
            bool negative = text[0] == '-';
            int start = negative || text[0] == '+' ? 1 : 0;
            if (start == text.Length) return Clamp(previous);
            int value = 0;
            for (int i = start; i < text.Length; i++)
            {
                if (text[i] < '0' || text[i] > '9') return Clamp(previous);
                // Saturate before multiplying so arbitrarily long input cannot overflow.
                if (value < Maximum) value = System.Math.Min(Maximum, value * 10 + text[i] - '0');
            }
            return negative ? Minimum : value;
        }
    }
}
