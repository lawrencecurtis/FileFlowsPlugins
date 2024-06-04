﻿#if(DEBUG)

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using AudioNodes.Tests;

namespace FileFlows.AudioNodes.Tests;
[TestClass]
public class AudioInfoTests: AudioTestBase
{
    const string file = @"/home/john/Music/test/test.wav";
    readonly string ffmpegExe = (OperatingSystem.IsLinux() ? "/usr/local/bin/ffmpeg" :  @"C:\utils\ffmpeg\ffmpeg.exe");
    readonly string ffprobe = (OperatingSystem.IsLinux() ? "/usr/local/bin/ffprobe" :  @"C:\utils\ffmpeg\ffprobe.exe");
    
    [TestMethod]
    public void AudioInfo_SplitTrack()
    {
        var args = GetNodeParameters(TestFile_Mp3);
        var af = new AudioFile();
        af.PreExecute(args);
        var result = af.Execute(args); // need to read the Audio info and set it

        Assert.AreEqual(1, result);

        var AudioInfo = args.Parameters["AudioInfo"] as AudioInfo;

        Assert.AreEqual(4, AudioInfo.Track);
    }

    [TestMethod]
    public void AudioInfo_NormalTrack()
    {

        const string file = @"\\oracle\Audio\Taylor Swift\Speak Now\Taylor Swift - Speak Now - 08 - Never Grow Up.mp3";
        const string ffmpegExe = @"C:\utils\ffmpeg\ffmpeg.exe";

        var args = new FileFlows.Plugin.NodeParameters(file, new TestLogger(), false, string.Empty, null);;
        args.GetToolPathActual = (string tool) => ffmpegExe;
        args.TempPath = @"D:\music\temp";

        var AudioInfo = new AudioInfoHelper(ffmpegExe, ffprobe, args.Logger).Read(args.WorkingFile);

        Assert.AreEqual(8, AudioInfo.Value.Track);
    }

    [TestMethod]
    public void AudioInfo_GetMetaData()
    {
        var logger = new TestLogger();
        foreach (string file in Directory.GetFiles(@"/home/john/Music/test"))
        {
            var args = new FileFlows.Plugin.NodeParameters(file, logger, false, string.Empty, null);
            args.GetToolPathActual = (string tool) => ffmpegExe;

            // laod the variables
            Assert.AreEqual(1, new AudioFile().Execute(args));

            var audio = new AudioInfoHelper(ffmpegExe, ffprobe, args.Logger).Read(args.WorkingFile).Value;

            string folder = args.ReplaceVariables("{audio.ArtistThe} ({audio.Year})");
            Assert.AreEqual($"{audio.Artist} ({audio.Date.Year})", folder);

            string fname = args.ReplaceVariables("{audio.Artist} - {audio.Album} - {audio.Track:##} - {audio.Title}");
            Assert.AreEqual($"{audio.Artist} - {audio.Track.ToString("00")} - {audio.Title}", fname);
        }
    }

    [TestMethod]
    public void AudioInfo_FileNameMetadata()
    {
        const string ffmpegExe = @"C:\utils\ffmpeg\ffmpeg.exe";
        var logger = new TestLogger();
        string file = @"\\jor-el\Audio\Meat Loaf\Bat out of Hell II- Back Into Hell… (1993)\Meat Loaf - Bat out of Hell II- Back Into Hell… - 03 - I’d Do Anything for Love (but I Won’t Do That).flac";
        
        var audio = new AudioInfo();

        new AudioInfoHelper(ffmpegExe, ffprobe, logger).ParseFileNameInfo(file, audio);

        Assert.AreEqual("Meat Loaf", audio.Artist);
        Assert.AreEqual("Bat out of Hell II- Back Into Hell…", audio.Album);
        Assert.AreEqual(1993, audio.Date.Year);
        Assert.AreEqual("I’d Do Anything for Love (but I Won’t Do That)", audio.Title);
        Assert.AreEqual(3, audio.Track);
    }
    
    

    [TestMethod]
    public void AudioInfo_Bitrate()
    {
        var logger = new TestLogger();
        var file = @"/home/john/Music/test/test.mp3";
        var args = new FileFlows.Plugin.NodeParameters(file, logger, false, string.Empty, null);
        args.GetToolPathActual = (string tool) => ffmpegExe;

        // load the variables
        Assert.AreEqual(1, new AudioFile().Execute(args));
        
        // convert to 192
        var convert = new ConvertAudio();
        convert.Bitrate = 192;
        convert.SkipIfCodecMatches = false;
        convert.Codec = "mp3";
        convert.PreExecute(args);
        int result = convert.Execute(args);
        Assert.AreEqual(1, result);

        var audio = new AudioInfoHelper(ffmpegExe, ffprobe, args.Logger).Read(args.WorkingFile).Value;
        Assert.AreEqual(192 * 1024, audio.Bitrate);

        var md = new Dictionary<string, object>();
        convert.SetAudioInfo(args, audio, md);
        
        Assert.AreEqual((192 * 1024).ToString(), md["audio.Bitrate"].ToString());
        
        // converting again should skip
        convert = new();
        convert.SkipIfCodecMatches = false;
        convert.Codec = "mp3";
        convert.Bitrate = 192;
        convert.PreExecute(args);
        result = convert.Execute(args);
        Assert.AreEqual(2, result);

        string log = logger.ToString();
    }
    
    [TestMethod]
    public void AudioFormatInfoTest()
    {
        string ffmpegOutput = @"{
            ""format"": {
                ""filename"": ""Aqua - Aquarium - 03 - Barbie Girl.flac"",
                ""nb_streams"": 1,
                ""nb_programs"": 0,
                ""format_name"": ""flac"",
                ""format_long_name"": ""raw FLAC"",
                ""start_time"": ""0.000000"",
                ""duration"": ""197.906667"",
                ""size"": ""25955920"",
                ""bit_rate"": ""1049218"",
                ""probe_score"": 100,
                ""tags"": {
                    ""TITLE"": ""Barbie Girl"",
                    ""ARTIST"": ""Aqua"",
                    ""ALBUM"": ""Aquarium"",
                    ""track"": ""3"",
                    ""DATE"": ""1997"",
                    ""GENRE"": ""Eurodance"",
                    ""TOTALTRACKS"": ""11"",
                    ""disc"": ""1"",
                    ""TOTALDISCS"": ""1""
                }
            }
        }";

        // Deserialize the JSON using System.Text.Json
        var result = FFprobeAudioInfo.Parse(ffmpegOutput);
        Assert.IsFalse(result.IsFailed);
        var audioFormatInfo = result.Value;

        Assert.AreEqual(1049218, audioFormatInfo.Bitrate);
        Assert.AreEqual("Barbie Girl", audioFormatInfo.Tags?.Title);
        Assert.AreEqual("Aqua", audioFormatInfo.Tags?.Artist);
        Assert.AreEqual("3", audioFormatInfo.Tags?.Track);
    }
}

#endif