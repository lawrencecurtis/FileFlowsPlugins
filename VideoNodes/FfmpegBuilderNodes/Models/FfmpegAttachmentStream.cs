namespace FileFlows.VideoNodes.FfmpegBuilderNodes.Models;

/// <summary>
/// FFmpeg Builder Attachment stream
/// </summary>
public class FfmpegAttachmentStream : FfmpegStream
{
    public AttachmentStream Stream { get; set; }

    public override bool HasChange => false;

    public override string[] GetParameters(GetParametersArgs args)
    {
        if (Deleted)
            return new string[] { };

        List<string> results= new List<string> { "-map", Stream.InputFileIndex + ":t:{sourceTypeIndex}", "-c:t:{index}", "copy" };
        return results.ToArray();
    }
}
