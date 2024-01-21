using FileFlows.VideoNodes.FfmpegBuilderNodes.Models;

namespace FileFlows.VideoNodes.FfmpegBuilderNodes;

/// <summary>
/// FFmpeg Builder flow element to set the original language as the default tracks
/// </summary>
public class FfmpegBuilderSetOriginalLanguageAsDefault: FfmpegBuilderNode
{
    /// <summary>
    /// Gets the help URL for the flow element
    /// </summary>
    public override string HelpUrl => "https://fileflows.com/docs/plugins/video-nodes/ffmpeg-builder/set-original-language-as-default";

    /// <summary>
    /// Gets the number of outputs of the flow element
    /// </summary>
    public override int Outputs => 2;

    /// <summary>
    /// Gets the icon of the flow element
    /// </summary>
    public override string Icon => "fas fa-globe";

    /// <summary>
    /// Gets or sets the stream type
    /// </summary>
    [Select(nameof(StreamTypeOptions), 1)]
    public string StreamType { get; set; }

    private static List<ListOption> _StreamTypeOptions;
    /// <summary>
    /// Gets the stream options to show in the UI
    /// </summary>
    public static List<ListOption> StreamTypeOptions
    {
        get
        {
            if (_StreamTypeOptions == null)
            {
                _StreamTypeOptions = new List<ListOption>
                {
                    new () { Label = "Audio", Value = "Audio" },
                    new () { Label = "Subtitle", Value = "Subtitle" },
                    new () { Label = "Both", Value = "Both" },
                };
            }
            return _StreamTypeOptions;
        }
    }

    /// <summary>
    /// Executes the flow element
    /// </summary>
    /// <param name="args">the flow parameters</param>
    /// <returns>the flow output to call next</returns>
    public override int Execute(NodeParameters args)
    {
        string originalLanguage;
        if (args.Variables.TryGetValue("OriginalLanguage", out object oValue) == false ||
            string.IsNullOrWhiteSpace(originalLanguage = oValue as string))
        {
            args.Logger?.ILog("OriginalLanguage variable was not set.");
            return 2;
        }
        args.Logger?.ILog("OriginalLanguage: " + originalLanguage);

        int changes = 0;
        if(StreamType is "Audio" or "Both")
        {
            changes += ProcessStreams(args, Model.AudioStreams, originalLanguage);
        }
        if(StreamType is "Subtitle" or "Both")
        {
            changes += ProcessStreams(args, Model.SubtitleStreams, originalLanguage);
        }

        return changes > 0 ? 1 : 2;
    }

    private int ProcessStreams<T>(NodeParameters args, List<T> streams, string originalLanguage) where T : FfmpegStream
    {
        if (streams?.Any() != true)
            return 0;
        
        int changed = 0;
        foreach (var stream in streams)
        {
            if (stream.Deleted)
                continue;

            bool isDefault = LanguageMatches(stream.Language, originalLanguage);
            if(isDefault)
                args.Logger?.ILog($"Stream '{stream.GetType().Name}' '{stream.Language}' set as default.");

            if (stream.IsDefault == isDefault)
                continue;
            
            stream.IsDefault = isDefault;
            ++changed;
        }

        return changed;
    }

    /// <summary>
    /// Tests if a language matches
    /// </summary>
    /// <param name="streamLanguage">the language of ths stream</param>
    /// <param name="testLanguage">the language to test</param>
    /// <returns>true if matches, otherwise false</returns>
    private bool LanguageMatches(string streamLanguage, string testLanguage)
    {
        if (string.IsNullOrWhiteSpace(testLanguage))
            return false;
        if (string.IsNullOrWhiteSpace(streamLanguage))
            return false;
        if (testLanguage.ToLowerInvariant().Contains(streamLanguage.ToLowerInvariant()))
            return true;
        try
        {
            if (LanguageHelper.GetIso2Code(streamLanguage) == LanguageHelper.GetIso2Code(testLanguage))
                return true;
        }
        catch (Exception)
        {
        }

        try
        {
            if (LanguageHelper.GetIso1Code(streamLanguage) == LanguageHelper.GetIso1Code(testLanguage))
                return true;
        }
        catch (Exception)
        {
        }

        try
        {
            var rgx = new Regex(testLanguage, RegexOptions.IgnoreCase);
            return rgx.IsMatch(streamLanguage);
        }
        catch (Exception)
        {
            return false;
        }
    }
}
