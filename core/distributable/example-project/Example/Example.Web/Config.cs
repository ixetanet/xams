namespace Example.Web;

public class Config
{
    public static readonly string? Environment = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
}