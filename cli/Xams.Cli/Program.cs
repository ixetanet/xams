// See https://aka.ms/new-console-template for more information
/*
 * Commands:
 * create {projectName}
 * create {projectName} -v {version}
 * create {projectName} -t {templateName} (default, api, nextjs)
 * create {projectName} -x {accessToken}
 * update {version}
 */

using Spectre.Console;
using Xams.Cli;

bool dev = false;

string apiUrl = dev ? "http://localhost:5277": "https://api.xams.io"; 


string cliKey = "xxxxxxx"; // This key is used to verify that the request is coming from the CLI

string[] actions = { "Create Api Project", "Create React \\ NextJS Project" };

var action = AnsiConsole.Prompt(
    new SelectionPrompt<string>()
        .Title("Select action")
        .PageSize(10)
        .MoreChoicesText("[grey](Use the arrow keys to select)[/]")
        .AddChoices(actions));

// Create Api Project
if (action == actions[0])
{
    await new CreateApi().Execute(new Download.DownloadOptions
    {
        CliApiKey = cliKey,
        ApiUrl = apiUrl,
        FileName = "aspnet",
        Mutations = null!
    });
}

// Create React \ NextJS Project
if (action == actions[1])
{
    await new CreateNextJs().Execute(new Download.DownloadOptions()
    {
        CliApiKey = cliKey,
        ApiUrl = apiUrl,
        FileName = "nextjs-app",
        Mutations = null!
    });
}


namespace Xams.Cli
{
    public class ApiResponse<T>
    {
        public bool succeeded { get; set; }
        public string friendlyMessage { get; set; }
        public string logMessage { get; set; }
        public T? data { get; set; }
    }

    public class ApiData
    {
        public string projectUrl { get; set; }
        public string xamsCoreUrl { get; set; }
    }
}