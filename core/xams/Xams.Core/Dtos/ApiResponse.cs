namespace Xams.Core.Dtos;

public class ApiResponse
{
    public bool succeeded { get; set; }
    public string? friendlyMessage { get; set; }
    public string? logMessage { get; set; }
    public dynamic? data { get; set; }
}