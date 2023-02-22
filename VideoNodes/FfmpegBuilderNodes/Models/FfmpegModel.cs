﻿namespace FileFlows.VideoNodes.FfmpegBuilderNodes.Models
{
    public class FfmpegModel
    {
        private List<FfmpegVideoStream> _VideoStreams = new List<FfmpegVideoStream>();
        /// <summary>
        /// Gets or sets the video streams
        /// </summary>
        public List<FfmpegVideoStream> VideoStreams
        {
            get => _VideoStreams;
            set => _VideoStreams = value ?? new List<FfmpegVideoStream>();
        }
        private List<FfmpegAudioStream> _AudioStreams = new List<FfmpegAudioStream>();
        /// <summary>
        /// Gets or sets the audio streams
        /// </summary>
        public List<FfmpegAudioStream> AudioStreams
        {
            get => _AudioStreams;
            set => _AudioStreams = value ?? new List<FfmpegAudioStream>();
        }
        private List<FfmpegSubtitleStream> _SubtitleStreams = new List<FfmpegSubtitleStream>();
        /// <summary>
        /// Gets or sets the subtitle streams
        /// </summary>
        public List<FfmpegSubtitleStream> SubtitleStreams
        {
            get => _SubtitleStreams;
            set => _SubtitleStreams = value ?? new List<FfmpegSubtitleStream>();
        }

        private List<FfmpegAttachmentStream> _AttachmentStreams = new ();
        /// <summary>
        /// Gets or sets the attachment streams
        /// </summary>
        public List<FfmpegAttachmentStream> AttachmentStreams
        {
            get => _AttachmentStreams;
            set => _AttachmentStreams = value ?? new List<FfmpegAttachmentStream>();
        }
        
        private List<string> _MetadataParameters = new List<string>();
        public List<string> MetadataParameters
        {
            get => _MetadataParameters;
            set => _MetadataParameters = value ?? new List<string>();
        }

        public string Extension { get; set; }

        private List<InputFile> _InputFiles = new List<InputFile>();
        public List<InputFile> InputFiles
        {
            get => _InputFiles;
            set => _InputFiles = value ?? new List<InputFile>();
        }

        private List<string> _CustomParameters = new List<string>();

        /// <summary>
        /// Gets or sets custom parameters to use in the FFMPEG Builder
        /// </summary>
        public List<string> CustomParameters
        {
            get => _CustomParameters;
            set => _CustomParameters = value ?? new List<string>();
        }

        /// <summary>
        /// Gets or sets if the builder should forcable execute even if nothing appears to have changed
        /// </summary>
        public bool ForceEncode { get; set; }

        /// <summary>
        /// Gets or sets the video information for this video file
        /// </summary>
        public VideoInfo VideoInfo => _VideoInfo;
        readonly VideoInfo _VideoInfo;

        public FfmpegModel(VideoInfo info)
        {
            this._VideoInfo = info;
        }

        internal static FfmpegModel CreateModel(VideoInfo info)
        {
            var model = new FfmpegModel(info);
            model.InputFiles.Add(new InputFile(info.FileName));
            foreach (var item in info.VideoStreams.Select((stream, index) => (stream, index)))
            {
                model.VideoStreams.Add(new FfmpegVideoStream
                {
                    Index = item.index,
                    Title = item.stream.Title,
                    Stream = item.stream,
                });
            }
            foreach (var item in info.AudioStreams.Select((stream, index) => (stream, index)))
            {
                model.AudioStreams.Add(new FfmpegAudioStream
                {
                    Index = item.index,
                    Title = item.stream.Title,
                    Language = item.stream.Language,
                    Stream = item.stream,
                });
            }
            foreach (var item in info.SubtitleStreams.Select((stream, index) => (stream, index)))
            {
                model.SubtitleStreams.Add(new FfmpegSubtitleStream
                {
                    Index = item.index,
                    Title = item.stream.Title,
                    Language = item.stream.Language,
                    Stream = item.stream,
                });
            }
            
            foreach (var item in info.Attachments.Select((stream, index) => (stream, index)))
            {
                model.AttachmentStreams.Add(new FfmpegAttachmentStream()
                {
                    Index = item.index,
                    Title = item.stream.Title,
                    Stream = item.stream
                });
            }

            if(info.FileName.ToLower().EndsWith(".mp4"))
                model.Extension = info.FileName.Substring(info.FileName.LastIndexOf(".") + 1);
            if (info.FileName.ToLower().EndsWith(".mkv"))
                model.Extension = info.FileName.Substring(info.FileName.LastIndexOf(".") + 1);

            return model;
        }
    }

    /// <summary>
    /// Input file 
    /// </summary>
    public class InputFile
    {
        /// <summary>
        /// Gets or sets the filename of the file
        /// </summary>
        public string FileName { get; set; }    
        /// <summary>
        /// Gets or sets if the file should be deleted after processing
        /// </summary>
        public bool DeleteAfterwards { get; set; }

        public InputFile(string fileName)
        {
            FileName = fileName;
        }
    }
}
