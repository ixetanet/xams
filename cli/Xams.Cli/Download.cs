using System.IO.Compression;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Spectre.Console;
using Xams.Cli.Utils;

namespace Xams.Cli;

public class Download
{
    public class DownloadOptions
    {
        public required string CliApiKey { get; set; }
        public required string ApiUrl { get; set; }
        public required string FileName { get; set; }
        public required Func<MutationsContext, Task> Mutations { get; set; }
        

    }

    public class MutationsContext
    {
        public required string BaseDirectory { get; set; }
        public required string ProjectName { get; set; }
        public required string ProjectDirectory { get; set; }
        public required string FileDownloadUrl { get; set; }
        public required string XamsCoreDownloadUrl { get; set; }

    }
    
    private bool _macFolderExists = false;
    private string _zipFileName = string.Empty;
    private string _baseDirectory = string.Empty;
    private Guid _correlationId = Guid.NewGuid();

    public async Task Execute(DownloadOptions options)
    {
        HttpClient httpClient = new HttpClient();
        string emailAddr = string.Empty;
        while (string.IsNullOrEmpty(emailAddr) || !IsValidEmail(emailAddr))
        {
            emailAddr = AnsiConsole.Ask<string>("[green]Email Address (A project key will be emailed to this address):[/]");
            if (!IsValidEmail(emailAddr))
            {
                AnsiConsole.MarkupLine($"Invalid email address");
            }
        }
        
        // post with a body 
        var content = new StringContent(JsonSerializer.Serialize(new { emailAddress = emailAddr, cliKey = options.CliApiKey }), Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync($"{options.ApiUrl}/cli/keyrequest", content);
        if (!response.IsSuccessStatusCode)
        {
            AnsiConsole.MarkupLine($"Error communicating with server");
            return;
        }
        
        string keyReqResults = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<string>>(keyReqResults);
        
        if (apiResponse is not { succeeded: true })
        {
            AnsiConsole.MarkupLine($"Unable to deserialize response from server");
            return;
        }

        string projectKey = string.Empty;
        while (string.IsNullOrEmpty(projectKey) || !Guid.TryParse(projectKey, out _))
        {
            string key = AnsiConsole.Ask<string>("[green]Project Key (Check your email for the project key):[/]");
            if (string.IsNullOrEmpty(key.Trim()) || !Guid.TryParse(key, out _))
            {
                Console.WriteLine("Invalid project key");
                continue;
            }
            projectKey = key;
        }
        
        string projectName = string.Empty;
        while (string.IsNullOrEmpty(projectName))
        {
            var name = AnsiConsole.Ask<string>("[green]Project Name:[/]");
            Regex regex = new Regex("^[a-zA-Z]+$");
            if (string.IsNullOrEmpty(name.Trim()) || !regex.IsMatch(name))
            {
                Console.WriteLine("Invalid project name, must be alphabetic characters only");
            }
            else
            {
                projectName = name;    
            }
        }
        

        await AnsiConsole.Status()
            .StartAsync("Creating project", async ctx =>
            {
                try
                {
                    _correlationId = Guid.NewGuid();
                    _zipFileName = $"_temp_xam_{_correlationId}.zip";
                    // _baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    _baseDirectory = Environment.CurrentDirectory;
                    _macFolderExists = Directory.Exists(Path.Combine(_baseDirectory, "__MACOSX"));

                    AnsiConsole.MarkupLine("Getting download url");
                    
                    var httpResponse = await httpClient.GetAsync($"{options.ApiUrl}/cli/link?projectKey={projectKey}&projectName={options.FileName}");
                    string linkContent = await httpResponse.Content.ReadAsStringAsync();
                    var linkApiResponse = JsonSerializer.Deserialize<ApiResponse<ApiData>>(linkContent);
                    if (linkApiResponse == null)
                    {
                        AnsiConsole.MarkupLine($"Error deserializing response from server");
                        return;
                    }
                    
                    if (linkApiResponse.succeeded == false)
                    {
                        AnsiConsole.MarkupLine(linkApiResponse.friendlyMessage);
                        return;
                    }

                    string projectUrl = linkApiResponse.data?.projectUrl!;
                    string xamsCoreUrl = linkApiResponse.data?.xamsCoreUrl!;

                    AnsiConsole.MarkupLine("Downloading Xams");
                    httpResponse = await httpClient.GetAsync(projectUrl);
                    if (!httpResponse.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Error communicating with server");
                        return;
                    }

                    using (var fs = File.Create(_zipFileName))
                    {
                        await httpResponse.Content.CopyToAsync(fs);
                    }

                    AnsiConsole.MarkupLine("Extracting");
                    ZipFile.ExtractToDirectory(_zipFileName, Path.Combine(_baseDirectory, _correlationId.ToString()));
                    Directory.Move( Path.Combine(_baseDirectory, _correlationId.ToString(), options.FileName), $"{_baseDirectory}/{projectName}");

                    await options.Mutations(new MutationsContext()
                    {
                        BaseDirectory = _baseDirectory,
                        ProjectName = projectName,
                        ProjectDirectory = Path.Combine(_baseDirectory, projectName),
                        FileDownloadUrl = projectUrl,
                        XamsCoreDownloadUrl = xamsCoreUrl
                    });

                    Cleanup();
                    
                    AnsiConsole.MarkupLine("Finished creating project");
                   
                }
                catch (Exception e)
                {
                    AnsiConsole.MarkupLine($"[red]An error occurred: {e.Message}[/]");
                    Cleanup();
                    throw;
                }
            });
    }
    
    private bool IsValidEmail(string email)
    {
        try
        {
            var mailAddress = new MailAddress(email);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private void Cleanup()
    {
        AnsiConsole.MarkupLine("Cleanup");
        File.Delete(_zipFileName);
        string macFolder = Path.Combine(_baseDirectory, "__MACOSX");
        if (!_macFolderExists && Directory.Exists(macFolder))
        {
            Directory.Delete(macFolder, true);
        }

        if (Directory.Exists(Path.Combine(_baseDirectory, _correlationId.ToString())))
        {
            Directory.Delete(Path.Combine(_baseDirectory, _correlationId.ToString()), true);
        }
    }
}