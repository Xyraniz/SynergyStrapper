using System;
using System.Collections.Generic;
using System.Linq;

namespace SynergyStrapper
{
    public enum FastFlagHealthSeverity
    {
        Warning,
        Error
    }

    public sealed record FastFlagHealthIssue(
        string Name,
        FastFlagHealthSeverity Severity,
        string Message
    );

    public static class FastFlagHealthCheck
    {
        private static readonly string[] ValidPrefixes =
        {
            "FFlag", "DFFlag", "SFFlag", "FInt", "DFInt", "FString", "DFString", "FLog", "DFLog"
        };

        private static readonly Regex BoolFilterPattern = new("^(?:true|false)(;[\\d]{1,})+$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex IntFilterPattern = new("^([\\d]{1,})?(;[\\d]{1,})+$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex StringFilterPattern = new("^[^;]*(;[\\d]{1,})+$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static bool IsValidName(string name)
        {
            return !String.IsNullOrWhiteSpace(name)
                && ValidPrefixes.Any(name.StartsWith)
                && name.All(x => Char.IsLetterOrDigit(x) || x == '_');
        }

        public static IReadOnlyList<FastFlagHealthIssue> Validate(IReadOnlyDictionary<string, object> flags)
        {
            var issues = new List<FastFlagHealthIssue>();
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var pair in flags.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
            {
                string name = pair.Key ?? String.Empty;
                string value = pair.Value?.ToString() ?? String.Empty;

                if (!names.Add(name))
                {
                    issues.Add(new FastFlagHealthIssue(
                        name,
                        FastFlagHealthSeverity.Warning,
                        "Another flag uses the same name without case sensitivity."
                    ));
                }

                if (String.IsNullOrWhiteSpace(name))
                {
                    issues.Add(new FastFlagHealthIssue(
                        name,
                        FastFlagHealthSeverity.Error,
                        "The flag name is empty."
                    ));
                    continue;
                }

                string? prefix = ValidPrefixes.FirstOrDefault(name.StartsWith);
                if (prefix is null)
                {
                    issues.Add(new FastFlagHealthIssue(
                        name,
                        FastFlagHealthSeverity.Error,
                        "The name does not use a supported FastFlag prefix."
                    ));
                    continue;
                }

                if (!name.All(x => Char.IsLetterOrDigit(x) || x == '_'))
                {
                    issues.Add(new FastFlagHealthIssue(
                        name,
                        FastFlagHealthSeverity.Error,
                        "The name contains characters that cannot be used in ClientAppSettings.json."
                    ));
                }

                if (name.EndsWith("_PlaceFilter", StringComparison.Ordinal)
                    || name.EndsWith("_DataCenterFilter", StringComparison.Ordinal))
                {
                    if (!ValidateFilter(prefix, value))
                    {
                        issues.Add(new FastFlagHealthIssue(
                            name,
                            FastFlagHealthSeverity.Error,
                            "The filter value does not match the expected format."
                        ));
                    }
                }
                else if ((prefix == "FFlag" || prefix == "DFFlag" || prefix == "SFFlag")
                    && !Boolean.TryParse(value, out _))
                {
                    issues.Add(new FastFlagHealthIssue(
                        name,
                        FastFlagHealthSeverity.Error,
                        "The value must be true or false for a boolean flag."
                    ));
                }
                else if ((prefix == "FInt" || prefix == "DFInt")
                    && !Int32.TryParse(value, out _))
                {
                    issues.Add(new FastFlagHealthIssue(
                        name,
                        FastFlagHealthSeverity.Error,
                        "The value must be a 32-bit integer for an integer flag."
                    ));
                }

                if (value.Contains('\r') || value.Contains('\n'))
                {
                    issues.Add(new FastFlagHealthIssue(
                        name,
                        FastFlagHealthSeverity.Warning,
                        "The value contains a line break and may not be accepted by Roblox."
                    ));
                }
            }

            return issues;
        }

        private static bool ValidateFilter(string prefix, string value)
        {
            if (prefix == "FFlag" || prefix == "DFFlag" || prefix == "SFFlag")
                return BoolFilterPattern.IsMatch(value);

            if (prefix == "FInt" || prefix == "DFInt")
                return IntFilterPattern.IsMatch(value);

            if (prefix == "FString" || prefix == "DFString" || prefix == "FLog" || prefix == "DFLog")
                return StringFilterPattern.IsMatch(value);

            return false;
        }
    }
}
