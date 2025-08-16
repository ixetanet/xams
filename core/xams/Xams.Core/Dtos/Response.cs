using Xams.Core.Interfaces;

namespace Xams.Core.Dtos;

public class Response<T> : IResponse<T>
{
    private bool _succeeded;

    public bool Succeeded
    {
        get
        {
            return _succeeded;
        }
        set
        {
            _succeeded = value;
        }
    }

    public string? FriendlyMessage { get; set; }
    public string? LogMessage { get; set; }
    public T? Data { get; set; }
    public ResponseType ResponseType { get; set; } = ResponseType.Json;
}

public class FileData
{
    public string FileName { get; set; } = "file";
        
    public Stream Stream { get; set; }
        
    public string ContentType { get; set; } = "application/octet-stream";
}

public enum ResponseType
{
    Json,
    File
}