﻿using FileFlows.VideoNodes.FfmpegBuilderNodes.Models;

namespace FileFlows.VideoNodes.FfmpegBuilderNodes;

public class FfmpegBuilderAudioTrackRemover: FfmpegBuilderNode
{
    public override string HelpUrl => "https://docs.fileflows.com/plugins/video-nodes/ffmpeg-builder/track-remover";

    public override string Icon => "fas fa-eraser";

    public override int Outputs => 2;


    [Select(nameof(StreamTypeOptions), 1)]
    public string StreamType { get; set; }

    [Boolean(2)]
    [ConditionEquals(nameof(StreamType), "Video", inverse: true)]
    public bool RemoveAll { get; set; }

    [NumberInt(3)]
    public int RemoveIndex { get; set; }


    [TextVariable(4)]
    [ConditionEquals(nameof(RemoveAll), false)]
    public string Pattern { get; set; }

    [Boolean(5)]
    [ConditionEquals(nameof(RemoveAll), false)]
    public bool NotMatching { get; set; }

    [Required]
    [Select(nameof(MatchTypes), 6)]
    [ConditionEquals(nameof(RemoveAll), false)]
    public MatchTypeOption MatchType { get; set; }

    private static List<ListOption> _MatchTypes;
    public static List<ListOption> MatchTypes
    {
        get
        {
            if (_MatchTypes == null)
            {
                _MatchTypes = new List<ListOption>
                {
                    new ListOption { Label = "Title", Value = MatchTypeOption.Title },
                    new ListOption { Label = "Language", Value = MatchTypeOption.Language },
                    new ListOption { Label = "Codec", Value = MatchTypeOption.Codec }
                };
            }
            return _MatchTypes;
        }
    }

    /// <summary>
    /// Left in for legacy reasons, will be removed later
    /// </summary>
    [Obsolete]
    public bool? UseLanguageCode
    {
        get => false;
        set
        {
            if ((int)this.MatchType > 0)
                return; // we can now ignore this value

            if (value == true)
                this.MatchType = MatchTypeOption.Language;
            else if(value == false)
                this.MatchType = MatchTypeOption.Title;
        }
    }

    private static List<ListOption> _StreamTypeOptions;
    public static List<ListOption> StreamTypeOptions
    {
        get
        {
            if (_StreamTypeOptions == null)
            {
                _StreamTypeOptions = new List<ListOption>
                {
                    new ListOption { Label = "Audio", Value = "Audio" },
                    new ListOption { Label = "Video", Value = "Video" },
                    new ListOption { Label = "Subtitle", Value = "Subtitle" }
                };
            }
            return _StreamTypeOptions;
        }
    }
    public override int Execute(NodeParameters args)
    {
        if(string.IsNullOrEmpty(StreamType) || StreamType.ToLower() == "audio")
            return RemoveTracks(Model.AudioStreams) ? 1 : 2;
        if (StreamType.ToLower() == "subtitle")
            return RemoveTracks(Model.SubtitleStreams) ? 1 : 2;
        if (StreamType.ToLower() == "video")
            return RemoveTracks(Model.VideoStreams) ? 1 : 2;

        return 2;
    }

    private bool RemoveTracks<T>(List<T> tracks) where T: FfmpegStream
    {
        bool removing = false;
        Regex? regex = null;
        int index = -1;
        Args.Logger.ILog("Using match type: " + MatchType);
        foreach (var track in tracks)
        {
            if (track.Deleted == false)
            {
                // only record indexes of tracks that have not been deleted
                ++index;
                if (index < RemoveIndex)
                    continue;
            }

            if (RemoveAll || string.IsNullOrEmpty(this.Pattern))
            {
                track.Deleted = true;
                removing = true;
                continue;
            }

            if (regex == null)
                regex = new Regex(this.Pattern, RegexOptions.IgnoreCase);

            string str = "";
            if(track is FfmpegAudioStream audio)
                str = MatchType == MatchTypeOption.Language ? audio.Stream.Language  :
                      MatchType == MatchTypeOption.Codec ? audio.Stream.Codec :
                      audio.Stream.Title;
            else if (track is FfmpegSubtitleStream subtitle)
                str = MatchType == MatchTypeOption.Language ? subtitle.Stream.Language :
                      MatchType == MatchTypeOption.Codec ? subtitle.Stream.Codec :
                      subtitle.Stream.Title;
            else if (track is FfmpegVideoStream video)
                str = MatchType == MatchTypeOption.Codec ? video.Stream.Codec : video.Stream.Title;

            Args.Logger.ILog("Testing string: " + str);
            if (string.IsNullOrEmpty(str) == false) // if empty we always use this since we have no info to go on
            {
                bool matches = regex.IsMatch(str);
                if (NotMatching)
                    matches = !matches;
                if (matches)
                {
                    track.Deleted = true;
                    removing = true;
                }
            }
        }
        return removing;
    }
}


public enum MatchTypeOption
{
    Title = 1,
    Language = 2,
    Codec = 3
};
