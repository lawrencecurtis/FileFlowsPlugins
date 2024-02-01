﻿using System.Globalization;
using FileFlows.VideoNodes.FfmpegBuilderNodes.Models;
using FileFlows.VideoNodes.Helpers;

namespace FileFlows.VideoNodes.FfmpegBuilderNodes;

/// <summary>
/// FFmpeg Builder: Track Sorter
/// </summary>
public class FfmpegBuilderTrackSorter : FfmpegBuilderNode
{
    /// <summary>
    /// Gets the number of output nodes
    /// </summary>
    public override int Outputs => 2;

    /// <summary>
    /// Gets the icon
    /// </summary>
    public override string Icon => "fas fa-sort-alpha-down";

    /// <summary>
    /// Gets the help URL
    /// </summary>
    public override string HelpUrl => "https://fileflows.com/docs/plugins/video-nodes/ffmpeg-builder/track-sorter";

    /// <summary>
    /// Gets or sets the stream type
    /// </summary>
    [Select(nameof(StreamTypeOptions), 1)]
    public string StreamType { get; set; }

    [KeyValue(1, nameof(SorterOptions))]
    [Required]
    public List<KeyValuePair<string, string>> Sorters { get; set; }

    private static List<ListOption> _StreamTypeOptions;

    /// <summary>
    /// Gets or sets the stream type options
    /// </summary>
    public static List<ListOption> StreamTypeOptions
    {
        get
        {
            if (_StreamTypeOptions == null)
            {
                _StreamTypeOptions = new List<ListOption>
                {
                    new() { Label = "Audio", Value = "Audio" },
                    new() { Label = "Subtitle", Value = "Subtitle" }
                };
            }

            return _StreamTypeOptions;
        }
    }

    private static List<ListOption> _SorterOptions;

    /// <summary>
    /// Gets or sets the sorter options
    /// </summary>
    public static List<ListOption> SorterOptions
    {
        get
        {
            if (_SorterOptions == null)
            {
                _SorterOptions = new List<ListOption>
                {
                    new() { Label = "Bitrate", Value = "Bitrate" },
                    new() { Label = "Bitrate Reversed", Value = "BitrateDesc" },
                    new() { Label = "Channels", Value = "Channels" },
                    new() { Label = "Channels Reversed", Value = "ChannelsDesc" },
                    new() { Label = "Codec", Value = "Codec" },
                    new() { Label = "Codec Reversed", Value = "CodecDesc" },
                    new() { Label = "Language", Value = "Language" },
                    new() { Label = "Language Reversed", Value = "LanguageDesc" },
                };
            }

            return _SorterOptions;
        }
    }

    /// <summary>
    /// Executes the flow element
    /// </summary>
    /// <param name="args">the node parameters</param>
    /// <returns>the next output node</returns>
    public override int Execute(NodeParameters args)
    {
        return 1;
    }

    /// <summary>
    /// Processes the streams 
    /// </summary>
    /// <param name="args">the node parameters</param>
    /// <param name="streams">the streams to process for deletion</param>
    /// <typeparam name="T">the stream type</typeparam>
    /// <returns>if any changes were made</returns>
    internal bool ProcessStreams<T>(NodeParameters args, List<T> streams, int sortIndex = 0) where T : FfmpegStream
    {
        if (streams?.Any() != true || Sorters?.Any() != true || sortIndex >= Sorters.Count)
            return false;

        var orderedStreams = SortStreams(args, streams);

        // Replace the unsorted items with the sorted ones
        for (int i = 0; i < streams.Count; i++)
        {
            streams[i] = orderedStreams[i];
        }
        
        return true;
    }

    internal List<T> SortStreams<T>(NodeParameters args, List<T> streams) where T : FfmpegStream
    {
        if (streams?.Any() != true || Sorters?.Any() != true)
            return streams;

        return streams.OrderBy(stream => GetSortKey(args, stream))
            .ToList();
    }

    private string GetSortKey<T>(NodeParameters args, T stream) where T : FfmpegStream
    {
        string sortKey = "";

        for (int i = 0; i < Sorters.Count; i++)
        {
            var sortValue = Math.Round(SortValue<T>(args, stream, Sorters[i])).ToString();
            // Trim the sort value to 15 characters
            string trimmedValue = sortValue[..Math.Min(sortValue.Length, 15)];

            // Pad the trimmed value with left zeros if needed
            string paddedValue = trimmedValue.PadLeft(15, '0');

            // Concatenate the padded value to the sort key
            sortKey += paddedValue;
        }

        return sortKey;
    }

    /// <summary>
    /// Tests if two lists are the same
    /// </summary>
    /// <param name="original">the original list</param>
    /// <param name="reordered">the reordered list</param>
    /// <typeparam name="T">the type of items</typeparam>
    /// <returns>true if the lists are the same, otherwise false</returns>
    public bool AreSame<T>(List<T> original, List<T> reordered) where T : FfmpegStream
    {
        for (int i = 0; i < reordered.Count; i++)
        {
            if (reordered[i] != original[i])
            {
                return false;
            }
        }

        return true;
    }
    
    
    
    
    
    /// <summary>
    /// Calculates the sort value for a stream property based on the specified sorter.
    /// </summary>
    /// <typeparam name="T">Type of the stream.</typeparam>
    /// <param name="args">the node parameters</param>
    /// <param name="stream">The stream instance.</param>
    /// <param name="sorter">The key-value pair representing the sorter.</param>
    /// <returns>The calculated sort value for the specified property and sorter.</returns>
    public static double SortValue<T>(NodeParameters args, T stream, KeyValuePair<string, string> sorter) where T : FfmpegStream
    {
        string property = sorter.Key;
        bool invert = property.EndsWith("Desc");
        if (invert)
            property = property[..^4]; // remove "Desc"
        
        string comparison = sorter.Value ?? string.Empty;

        if (comparison.StartsWith("{") && comparison.EndsWith("}"))
        {
            comparison = comparison[1..^1];
            if (args?.Variables?.TryGetValue(comparison, out object variable) == true)
                comparison = variable?.ToString() ?? string.Empty;
            else
                comparison = string.Empty;
        }
        else if (args?.Variables?.TryGetValue(comparison, out object oVariable) == true)
        {
            comparison = oVariable?.ToString() ?? string.Empty;
        }

        if (property == nameof(stream.Language))
        {
            object oOriginalLanguage = null;
            args?.Variables?.TryGetValue("OriginalLanguage", out oOriginalLanguage);
            var originalLanguage = LanguageHelper.GetIso2Code(oOriginalLanguage?.ToString() ?? string.Empty);
            comparison = string.Join("|",
                comparison.Split('|', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x =>
                    {
                        if (x?.ToLowerInvariant()?.StartsWith("orig") == true)
                            return originalLanguage;
                        return LanguageHelper.GetIso2Code(x);
                    }).Where(x => string.IsNullOrWhiteSpace(x) == false).ToArray());
        }

        var value = property switch
        {
            nameof(FfmpegStream.Codec) => stream.Codec,
            nameof(AudioStream.Bitrate) => (stream is FfmpegAudioStream audioStream) ? audioStream?.Stream?.Bitrate : null,
            nameof(FfmpegStream.Language) => LanguageHelper.GetIso2Code(stream.Language),
            _ => stream.GetType().GetProperty(property)?.GetValue(stream, null)
        };

        double result;

        if (value != null && value is string == false && string.IsNullOrWhiteSpace(comparison) &&
            double.TryParse(value.ToString(), out double dblValue))
        {
            // invert the bits of dbl value and return that
            result = dblValue;
        }
        else if (IsMathOperation(comparison))
            result = ApplyMathOperation(value.ToString(), comparison) ? 0 : 1;
        else if (IsRegex(comparison))
            result = Regex.IsMatch(value.ToString(), comparison, RegexOptions.IgnoreCase) ? 0 : 1;
        else if (value != null && double.TryParse(value.ToString(), out double dbl))
            result = dbl;
        else
            result = string.Equals(value?.ToString() ?? string.Empty, comparison ?? string.Empty, StringComparison.OrdinalIgnoreCase) ? 0 : 1;

        return invert ? InvertBits(result) : result;
    }

    /// <summary>
    /// Adjusts the comparison string by handling common mistakes in units and converting them into full numbers.
    /// </summary>
    /// <param name="comparisonValue">The original comparison string to be adjusted.</param>
    /// <returns>The adjusted comparison string with corrected units or the original comparison if no adjustments are made.</returns>
    private static string AdjustComparisonValue(string comparisonValue)
    {
        if (string.IsNullOrWhiteSpace(comparisonValue))
            return string.Empty;
        
        string adjustedComparison = comparisonValue.ToLower().Trim();

        // Handle common mistakes in units
        if (adjustedComparison.EndsWith("mbps"))
        {
            // Make an educated guess for Mbps to kbps conversion
            return adjustedComparison[..^4] switch
            {
                { } value when double.TryParse(value, out var numericValue) => (numericValue * 1_000_000).ToString(
                    CultureInfo.InvariantCulture),
                _ => comparisonValue
            };
        }
        if (adjustedComparison.EndsWith("kbps"))
        {
            // Make an educated guess for kbps to bps conversion
            return adjustedComparison[..^4] switch
            {
                { } value when double.TryParse(value, out var numericValue) => (numericValue * 1_000).ToString(
                    CultureInfo.InvariantCulture),
                _ => comparisonValue
            };
        }
        if (adjustedComparison.EndsWith("gb"))
        {
            // Make an educated guess for GB to bytes conversion
            return adjustedComparison[..^2] switch
            {
                { } value when double.TryParse(value, out var numericValue) => (numericValue * Math.Pow(1024, 3))
                    .ToString(CultureInfo.InvariantCulture),
                _ => comparisonValue
            };
        }
        if (adjustedComparison.EndsWith("tb"))
        {
            // Make an educated guess for TB to bytes conversion
            return adjustedComparison[..^2] switch
            {
                { } value when double.TryParse(value, out var numericValue) => (numericValue * Math.Pow(1024, 4))
                    .ToString(CultureInfo.InvariantCulture),
                _ => comparisonValue
            };
        }

        return comparisonValue;
    }

    /// <summary>
    /// Inverts the bits of a double value.
    /// </summary>
    /// <param name="value">The double value to invert.</param>
    /// <returns>The inverted double value.</returns>
    private static double InvertBits(double value)
    {
        // Convert the double to a string with 15 characters above the decimal point
        string stringValue = Math.Round(value, 0).ToString("F0");

        // Invert the digits and pad left with zeros
        char[] charArray = stringValue.PadLeft(15, '0').ToCharArray();
        for (int i = 0; i < charArray.Length; i++)
        {
            charArray[i] = (char)('9' - (charArray[i] - '0'));
        }

        // Parse the inverted string back to a double
        double invertedDouble;
        if (double.TryParse(new string(charArray), out invertedDouble))
        {
            return invertedDouble;
        }
        else
        {
            // Handle parsing error
            throw new InvalidOperationException("Failed to parse inverted double string.");
        }
    }
    
    /// <summary>
    /// Checks if the comparison string represents a mathematical operation.
    /// </summary>
    /// <param name="comparison">The comparison string to check.</param>
    /// <returns>True if the comparison is a mathematical operation, otherwise false.</returns>
    private static bool IsMathOperation(string comparison)
    {
        // Check if the comparison string starts with <=, <, >, >=, ==, or =
        return new[] { "<=", "<", ">", ">=", "==", "=" }.Any(comparison.StartsWith);
    }
    
    /// <summary>
    /// Checks if the comparison string represents a regular expression.
    /// </summary>
    /// <param name="comparison">The comparison string to check.</param>
    /// <returns>True if the comparison is a regular expression, otherwise false.</returns>
    private static bool IsRegex(string comparison)
    {
        return new[] { "?", "|", "^", "$" }.Any(ch => comparison.Contains(ch));
    }

    /// <summary>
    /// Applies a mathematical operation to the value based on the specified operation string.
    /// </summary>
    /// <param name="value">The value to apply the operation to.</param>
    /// <param name="operation">The operation string representing the mathematical operation.</param>
    /// <returns>True if the mathematical operation is successful, otherwise false.</returns>
    private static bool ApplyMathOperation(string value, string operation)
    {
        // This is a basic example; you may need to handle different operators
        switch (operation.Substring(0, 2))
        {
            case "<=":
                return Convert.ToDouble(value) <= Convert.ToDouble(AdjustComparisonValue(operation[2..]));
            case ">=":
                return Convert.ToDouble(value) >= Convert.ToDouble(AdjustComparisonValue(operation[2..]));
            case "==":
                return Math.Abs(Convert.ToDouble(value) - Convert.ToDouble(AdjustComparisonValue(operation[2..]))) < 0.05f;
            case "!=":
                return Math.Abs(Convert.ToDouble(value) - Convert.ToDouble(AdjustComparisonValue(operation[2..]))) > 0.05f;
        }

        switch (operation.Substring(0, 1))
        {
            case "<":
                return Convert.ToDouble(value) < Convert.ToDouble(AdjustComparisonValue(operation[1..]));
            case ">":
                return Convert.ToDouble(value) > Convert.ToDouble(AdjustComparisonValue(operation[1..]));
            case "=":
                return Math.Abs(Convert.ToDouble(value) - Convert.ToDouble(AdjustComparisonValue(operation[1..]))) < 0.05f;
        }

        return false;
    }
}