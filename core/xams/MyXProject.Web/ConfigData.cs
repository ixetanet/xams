namespace MyXProject.Data;

public static class ConfigData
{
    private static ConfigRecords? _configRecords;

    public static ConfigRecords Get()
    {
        if (_configRecords != null)
        {
            return _configRecords;
        }

        using (DataContext db = new DataContext())
        {
            _configRecords = new ConfigRecords()
            {

            };    
        }
        

        return _configRecords;
    }
    
    public class ConfigRecords
    {

    }
}