using Microsoft.UI.Xaml.Data;
using System;
using System.Globalization;

namespace HGEngineGUI.Converters
{
    public sealed class MacroToDisplayConverter : IValueConverter
    {
        // Convenience helper for non-binding scenarios
        public static string ToDisplay(string? value)
        {
            try
            {
                var s = value ?? string.Empty;
                if (string.IsNullOrWhiteSpace(s)) return string.Empty;
                s = TrimKnownPrefixes(s);
                s = s.Replace('_', ' ');
                return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(s.ToLowerInvariant());
            }
            catch { return value ?? string.Empty; }
        }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            try
            {
                return ToDisplay(value as string);
            }
            catch { return value?.ToString() ?? string.Empty; }
        }

        private static string TrimKnownPrefixes(string s)
        {
            static string Strip(string src, string pre) => src.StartsWith(pre, StringComparison.Ordinal) ? src.Substring(pre.Length) : src;
            s = Strip(s, "SPECIES_");
            s = Strip(s, "TYPE_");
            s = Strip(s, "ABILITY_");
            s = Strip(s, "EGG_GROUP_");
            s = Strip(s, "GROWTH_");
            s = Strip(s, "ITEM_");
            s = Strip(s, "TRAINERCLASS_");
            s = Strip(s, "MOVE_");
            s = Strip(s, "NATURE_");
            s = Strip(s, "F_"); // AI flags
            s = Strip(s, "RANGE_");
            s = Strip(s, "FLAG_");
            s = Strip(s, "SPLIT_");
            s = Strip(s, "CONTEST_");
            return s;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}


