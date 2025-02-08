namespace Xams.Cli.Utils;

public class ReplaceUtil
{
    public static void ReplaceTextInFiles(string directoryPath, string searchText, string? replaceText)
    {
        foreach (string file in Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories))
        {
            string content = File.ReadAllText(file);
            string updatedContent = content.Replace(searchText, replaceText);
            File.WriteAllText(file, updatedContent);
        }
    }

    public static void SearchAndReplaceFileName(string directoryPath, string searchText, string? replaceText)
    {
        foreach (string file in Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories))
        {
            string? directory = Path.GetDirectoryName(file);
            string fileName = Path.GetFileName(file);

            if (fileName.Contains(searchText))
            {
                string newFileName = fileName.Replace(searchText, replaceText);
                if (directory != null)
                {
                    string newFilePath = Path.Combine(directory, newFileName);
                    File.Move(file, newFilePath);
                }
            }
        }
    }

    public static void SearchAndReplaceFolderName(string directoryPath, string searchText, string? replaceText)
    {
        foreach (string dir in Directory.GetDirectories(directoryPath, "*", SearchOption.AllDirectories))
        {
            string directoryName = new DirectoryInfo(dir).Name;
            if (directoryName.Contains(searchText))
            {
                string newDirectoryName = directoryName.Replace(searchText, replaceText);
                string? parentDir = Directory.GetParent(dir)?.FullName;
                if (parentDir != null)
                {
                    string newDir = Path.Combine(parentDir, newDirectoryName);
                    Directory.Move(dir, newDir);
                }
            }
        }
    }
}