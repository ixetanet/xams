using Xams.Cli.Utils;

namespace Xams.Cli;

public class CreateNextJs
{
    public async Task Execute(Download.DownloadOptions options)
    {
        options.Mutations = async (context) =>
        {
            ReplaceUtil.ReplaceTextInFiles(context.ProjectDirectory, "  \"name\": \"my-app\",", $"  \"name\": \"{context.ProjectName}\",");
        };
        
        await new Download().Execute(options);
    }
}