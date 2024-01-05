﻿using FileFlows.Plugin;
using FileFlows.Plugin.Attributes;

namespace FileFlows.AudioNodes
{
    public class ConvertToMP3 : ConvertNode
    {
        public override string HelpUrl => "https://fileflows.com/docs/plugins/audio-nodes/convert-to-mp3";
        protected override string Extension => "mp3";
    }
    public class ConvertToWAV : ConvertNode
    {
        public override string HelpUrl => "https://fileflows.com/docs/plugins/audio-nodes/convert-to-wav";
        protected override string Extension => "wav";
        
        private static List<ListOption> _BitrateOptions;
        public new static List<ListOption> BitrateOptions
        {
            get
            {
                if (_BitrateOptions == null)
                {
                    _BitrateOptions = new List<ListOption>
                    {
                        new () { Label = "Automatic", Value = 0 },
                        new () { Label = "Same as source", Value = -1 },
                        new () { Label = "64 Kbps", Value = 64},
                        new () { Label = "96 Kbps", Value = 96},
                        new () { Label = "128 Kbps", Value = 128},
                        new () { Label = "160 Kbps", Value = 160},
                        new () { Label = "192 Kbps", Value = 192},
                        new () { Label = "224 Kbps", Value = 224},
                        new () { Label = "256 Kbps", Value = 256},
                        new () { Label = "288 Kbps", Value = 288},
                        new () { Label = "320 Kbps", Value = 320},
                    };
                }
                return _BitrateOptions;
            }
        }
    }

    public class ConvertToAAC : ConvertNode
    {
        public override string HelpUrl => "https://fileflows.com/docs/plugins/audio-nodes/convert-to-aac";
        protected override string Extension => "aac";

        /// <summary>
        /// Gets or sets if high efficiency should be used
        /// </summary>
        [Boolean(5)]
        public bool HighEfficiency { get => base.HighEfficiency; set =>base.HighEfficiency = value; }
        
        protected override bool SetId3Tags => true;
    }
    public class ConvertToOGG: ConvertNode
    {
        public override string HelpUrl => "https://fileflows.com/docs/plugins/audio-nodes/convert-to-ogg";
        protected override string Extension => "ogg";
        public static List<ListOption> BitrateOptions => ConvertNode.BitrateOptions;
    }

    public class ConvertAudio : ConvertNode
    {
        protected override string Extension => Codec;
        public override string HelpUrl => "https://fileflows.com/docs/plugins/audio-nodes/convert-audio";

        public static List<ListOption> BitrateOptions => ConvertNode.BitrateOptions;

        [Select(nameof(CodecOptions), 0)]
        public string Codec { get; set; }

        /// <summary>
        /// Gets or sets if high efficiency should be used
        /// </summary>
        [Boolean(5)]
        [ConditionEquals(nameof(Codec), "aac")]
        public bool HighEfficiency { get => base.HighEfficiency; set =>base.HighEfficiency = value; }

        private static List<ListOption> _CodecOptions;
        public static List<ListOption> CodecOptions
        {
            get
            {
                if (_CodecOptions == null)
                {
                    _CodecOptions = new List<ListOption>
                    {
                        new () { Label = "AAC", Value = "aac"},
                        new () { Label = "MP3", Value = "MP3"},
                        new () { Label = "OGG", Value = "ogg"},
                        new () { Label = "WAV", Value = "wav"},
                    };
                }
                return _CodecOptions;
            }
        }

        public override int Execute(NodeParameters args)
        {
            AudioInfo AudioInfo = GetAudioInfo(args);
            if (AudioInfo == null)
                return -1;

            string ffmpegExe = GetFFmpeg(args);
            if (string.IsNullOrEmpty(ffmpegExe))
                return -1;
            
            return base.Execute(args);

        }
    }

    public abstract class ConvertNode:AudioNode
    {
        protected abstract string Extension { get; }
        /// <summary>
        /// Gets or sets if using high efficiency
        /// </summary>
        protected bool HighEfficiency { get; set; }

        protected long GetSourceBitrate(NodeParameters args)
        {
            var info = GetAudioInfo(args);
            return info.Bitrate;
        }

        protected virtual bool SetId3Tags => false;

        public override int Inputs => 1;
        public override int Outputs => 2;

        protected virtual List<string> GetArguments(NodeParameters args, out string? extension)
        {
            string Codec = Extension;
            extension = null;
            string codecKey = Codec + "_codec";
            string codec = args.GetToolPathActual(codecKey);
            if (codec == "mp3")
                extension = "mp3";
            if (codec == "libvorbis" || codec == "ogg")
            {
                codec = "libvorbis";
                extension = "ogg";
            }

            if (codec == codecKey || string.IsNullOrWhiteSpace(codec))
            {
                codec = Codec switch
                {
                    "ogg" => "libvorbis",
                    "wav" => "pcm_s16le",
                    _ => Codec.ToLower()
                };
            }

            int bitrate = Bitrate;
            if (Codec.ToLowerInvariant() == "mp3" ||  Codec.ToLowerInvariant() ==  "ogg" || Codec.ToLowerInvariant() == "aac")
            {
                if (bitrate is >= 10 and <= 20)
                {
                    bitrate = (Bitrate - 10);
                    if (Codec.ToLowerInvariant() == "mp3")
                    {
                        // ogg is reversed
                        bitrate = 10 - bitrate;
                    }


                    args.Logger?.ILog($"Using variable bitrate setting '{bitrate}' for codec '{Codec}'");

                    List<string> results;

                    if (codec == "libfdk_aac")
                    {
                        results = new()
                        {
                            "-c:a",
                            codec,
                            "-vbr",
                            Math.Min(Math.Max(1, bitrate / 2), 5).ToString()
                        };
                    }
                    else
                    {
                        results = new()
                        {
                            "-c:a",
                            codec,
                            "-qscale:a",
                            bitrate.ToString()
                        };
                    }

                    if (Codec == "aac" && HighEfficiency)
                    {
                        extension = "m4a";
                        results.AddRange(new[] { "-profile:a", "aac_he_v2" });
                    }

                    return results;
                }
            }
            else if(bitrate is > 10 and <= 20)
            {
                throw new Exception("Variable bitrate not supported in codec: " + Codec);
            }

            if (bitrate == 0)
            {
                // automatic
                return new List<string>
                {
                    "-c:a",
                    codec
                };
            }
            
            return new List<string>
            {
                "-c:a",
                codec,
                "-ab",
                (bitrate == -1 ? GetSourceBitrate(args).ToString() : bitrate + "k")
            };
        }

        /// <summary>
        /// Gets the type of flow element
        /// </summary>
        public override FlowElementType Type => FlowElementType.Process;

        /// <summary>
        /// Gets or sets the bitrate for the converted audio
        /// </summary>
        [Select(nameof(BitrateOptions), 1)]
        public int Bitrate { get; set; }
        
        /// <summary>
        /// Gets or sets if the audio should be normalized
        /// </summary>
        [Boolean(3)]
        public bool Normalize { get; set; }

        /// <summary>
        /// Gets or sets if it should be skipped if the codec is the same
        /// </summary>
        [Boolean(4)]
        [ConditionEquals(nameof(Normalize), true, inverse: true)]
        public bool SkipIfCodecMatches { get; set; }

        private static List<ListOption> _BitrateOptions;

        /// <summary>
        /// Gets the bitrate options to show to the user
        /// </summary>
        public static List<ListOption> BitrateOptions
        {
            get
            {
                if (_BitrateOptions == null)
                {
                    _BitrateOptions = new List<ListOption>
                    {
                        new () { Label = "Automatic", Value = 0 },
                        new () { Label = "Same as source", Value = -1 },
                        
                        new () { Label = "Constant Bitrate", Value = "###GROUP###" },
                        new () { Label = "64 Kbps", Value = 64},
                        new () { Label = "96 Kbps", Value = 96},
                        new () { Label = "128 Kbps", Value = 128},
                        new () { Label = "160 Kbps", Value = 160},
                        new () { Label = "192 Kbps", Value = 192},
                        new () { Label = "224 Kbps", Value = 224},
                        new () { Label = "256 Kbps", Value = 256},
                        new () { Label = "288 Kbps", Value = 288},
                        new () { Label = "320 Kbps", Value = 320},
                        
                        new () { Label = "Variable Bitrate", Value = "###GROUP###" },
                        new () { Label = "0 (Lowest Quality)", Value = 10},
                        new () { Label = "1", Value = 11},
                        new () { Label = "2", Value = 12},
                        new () { Label = "3", Value = 13},
                        new () { Label = "4", Value = 14},
                        new () { Label = "5 (Good Quality)", Value = 15},
                        new () { Label = "6", Value = 16},
                        new () { Label = "7", Value = 17},
                        new () { Label = "8", Value = 18},
                        new () { Label = "9", Value = 19},
                        new () { Label = "10 (Highest Quality)", Value = 20},
                        
                    };
                }
                return _BitrateOptions;
            }
        }

        /// <summary>
        /// Executes the flow element
        /// </summary>
        /// <param name="args">the node parameters</param>
        /// <returns>the output to call next</returns>
        public override int Execute(NodeParameters args)
        {
            AudioInfo AudioInfo = GetAudioInfo(args);
            if (AudioInfo == null)
                return -1;
            
            string ffmpegExe = GetFFmpeg(args);
            if (string.IsNullOrEmpty(ffmpegExe))
                return -1;

            if(Normalize == false && AudioInfo.Codec?.ToLower() == Extension?.ToLower())
            {
                if (SkipIfCodecMatches)
                {
                    args.Logger?.ILog($"Audio file already '{Extension}' at bitrate '{AudioInfo.Bitrate} bps', and set to skip if codec matches");
                    return 2;
                }

                args.Logger?.ILog($"Comparing bitrate {AudioInfo.Bitrate} is less than or equal to {(Bitrate * 1000)}");
                if(AudioInfo.Bitrate <= Bitrate * 1000) // this bitrate is in Kbps, whereas AudioInfo.Bitrate is bytes per second
                {
                    args.Logger?.ILog($"Audio file already '{Extension}' at bitrate '{AudioInfo.Bitrate} bps ({(AudioInfo.Bitrate / 1000)} KBps)'");
                    return 2;
                }
            }
            //AudioInfo AudioInfo = GetAudioInfo(args);
            //if (AudioInfo == null)
            //    return -1;
            // if (Bitrate != 0 && Bitrate != -1 && (Bitrate < 64 || Bitrate > 320))
            // {
            //     args.Logger?.ILog("Bitrate not set or invalid, setting to 192kbps");
            //     Bitrate = 192;
            // }


            var ffArgs = GetArguments(args, out string extension);
            string outputFile = Path.Combine(args.TempPath,
                Guid.NewGuid().ToString() + "." + (extension?.EmptyAsNull() ?? Extension));
            
            ffArgs.Insert(0, "-hide_banner");
            ffArgs.Insert(1, "-y"); // tells ffmpeg to replace the file if already exists, which it shouldnt but just incase
            ffArgs.Insert(2, "-i");
            ffArgs.Insert(3, args.WorkingFile);
            ffArgs.Insert(4, "-vn"); // disables video


            if (Normalize)
            {
                var twoPass =  AudioFileNormalization.DoTwoPass(args, ffmpegExe);
                if (twoPass.Success)
                {
                    ffArgs.Add("-af");
                    ffArgs.Add(twoPass.Normalization);
                }
            }

            ffArgs.Add(outputFile);

            args.Logger?.ILog("FFArgs: " + String.Join(" ", ffArgs.Select(x => x.IndexOf(" ") > 0 ? "\"" + x + "\"" : x).ToArray()));

            var result = args.Execute(new ExecuteArgs
            {
                Command = ffmpegExe,
                ArgumentList = ffArgs.ToArray()
            });

            if(result.ExitCode != 0)
            {
                args.Logger?.ELog("Invalid exit code detected: " + result.ExitCode);
                return -1;
            }

            //CopyMetaData(outputFile, args.FileName);

            args.SetWorkingFile(outputFile);

            // update the Audio file info
            if (ReadAudioFileInfo(args, ffmpegExe, args.WorkingFile))
                return 1;

            return -1;
        }

        //private void CopyMetaData(string outputFile, string originalFile)
        //{
        //    Track original = new Track(originalFile);
        //    Track dest = new Track(outputFile);
                        
        //    dest.Album = original.Album;
        //    dest.AlbumArtist = original.AlbumArtist;
        //    dest.Artist = original.Artist;
        //    dest.Comment = original.Comment;
        //    dest.Composer= original.Composer;
        //    dest.Conductor = original.Conductor;
        //    dest.Copyright = original.Copyright;
        //    dest.Date = original.Date;
        //    dest.Description= original.Description;
        //    dest.DiscNumber= original.DiscNumber;
        //    dest.DiscTotal = original.DiscTotal;
        //    if (original.EmbeddedPictures?.Any() == true)
        //    {
        //        foreach (var pic in original.EmbeddedPictures)
        //            dest.EmbeddedPictures.Add(pic);
        //    }
        //    dest.Genre= original.Genre;
        //    dest.Lyrics= original.Lyrics;
        //    dest.OriginalAlbum= original.OriginalAlbum;
        //    dest.OriginalArtist = original.OriginalArtist;
        //    dest.Popularity= original.Popularity;
        //    dest.Publisher= original.Publisher;
        //    dest.PublishingDate= original.PublishingDate;
        //    dest.Title= original.Title;
        //    dest.TrackNumber= original.TrackNumber;
        //    dest.TrackTotal= original.TrackTotal;
        //    dest.Year= original.Year;
        //    foreach (var key in original.AdditionalFields.Keys)
        //    {
        //        if(dest.AdditionalFields.ContainsKey(key))
        //            dest.AdditionalFields[key] = original.AdditionalFields[key];
        //        else
        //            dest.AdditionalFields.Add(key, original.AdditionalFields[key]);
        //    }

        //    dest.Save();
        //}
    }
}
