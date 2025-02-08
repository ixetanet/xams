namespace Xams.Cli.Utils;

public class CommandLineParser
{
    public string? Command { get; private set; }
    public string? ProjectName { get; private set; }
    private Dictionary<string?, string> parameters = new();
    private HashSet<string?> validFlags = new() { "-v", "-t" };

    public CommandLineParser(string?[]? args)
    {
        if (args == null || args.Length == 0 || args.Length < 2)
        {
            return;
        }

        Command = args[0];
        ProjectName = args[1];
        
        if (args.Length == 2)
        {
            return;
        }
        
        for (int i = 2; i < args.Length; i++)
        {
            string? currentArg = args[i];

            // Check if it's a flag and the next element exists
            if (validFlags.Contains(currentArg) && i + 1 < args.Length)
            {
                string? value = args[i + 1];
                parameters[currentArg] = value;
                i++; // Skip the next element as it's already processed
            }
            else
            {
                Console.WriteLine($"Invalid or incomplete argument: {currentArg}");
            }
        }
    }

    public string GetParameter(string? flag)
    {
        if (parameters.TryGetValue(flag, out string value))
        {
            return value;
        }

        return null;
    }
}