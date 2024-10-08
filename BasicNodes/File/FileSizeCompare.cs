using FileFlows.Plugin;

namespace FileFlows.BasicNodes.File;

/// <summary>
/// Flow element that compares the file size of the original file and the current working file
/// </summary>
public class FileSizeCompare : Node
{
    /// <inheritdoc />
    public override int Inputs => 1;
    /// <inheritdoc />
    public override int Outputs => 3;
    /// <inheritdoc />
    public override FlowElementType Type => FlowElementType.Logic;
    /// <inheritdoc />
    public override string Icon => "fas fa-sitemap";
    /// <inheritdoc />
    public override string HelpUrl => "https://fileflows.com/docs/plugins/basic-nodes/file-size-compare";


    /// <inheritdoc />
    public override int Execute(NodeParameters args)
    {
        var result = args.FileService.FileSize(args.FileName);
        long origSize = result.ValueOrDefault;
        if (result.IsFailed)
        {
            // try get from variables
            if (args.Variables.TryGetValue("file.Orig.Size", out object? value) && value is long tSize && tSize > 0)
            {
                origSize = tSize;
            }
            else
            {
                args.Logger?.ELog("Original file does not exists, cannot check size");
                return -1;
            }
        }

        //FileInfo fiWorkingFile = new FileInfo(args.WorkingFile);
        result = args.FileService.FileSize(args.WorkingFile);
        long wfSize = result.ValueOrDefault;
        if (result.IsFailed)
        {
            if (args.WorkingFileSize > 0)
            {
                wfSize = args.WorkingFileSize;
            }
            else
            {
                args.Logger?.ELog("Working file does not exists, cannot check size");
                return -1;
            }
        }
        

        args.Logger?.ILog($"Original File Size: {origSize:n0}");
        args.Logger?.ILog($"Working File Size: {wfSize:n0}");


        if (wfSize > origSize)
        {
            args.Logger?.ILog("Working file is larger than original");
            return 3;
        }
        if (origSize == wfSize)
        {
            args.Logger?.ILog("Working file is same size as the original");
            return 2;
        }
        args.Logger?.ILog("Working file is smaller than original");
        return 1;
    }
}