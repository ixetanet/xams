using System.IO.Compression;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Spectre.Console;
using Xams.Cli.Utils;

namespace Xams.Cli;

public class CreateApi
{
    public async Task Execute(Download.DownloadOptions options)
    {
        options.Mutations = async (context) =>
        {
            ReplaceUtil.ReplaceTextInFiles(context.ProjectDirectory, "MyXProject", context.ProjectName);
            ReplaceUtil.ReplaceTextInFiles(context.ProjectDirectory, "MyXProject".ToLower(),
                context.ProjectName.ToLower());
            ReplaceUtil.SearchAndReplaceFileName(context.ProjectDirectory, "MyXProject", context.ProjectName);
            ReplaceUtil.SearchAndReplaceFolderName(context.ProjectDirectory, "MyXProject", context.ProjectName);
        };
        
        await new Download().Execute(options);
    }
}