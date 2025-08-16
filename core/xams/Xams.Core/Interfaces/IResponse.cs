namespace Xams.Core.Interfaces;

public interface IResponse<T>
{
    public bool Succeeded { get; set; }
    public string? FriendlyMessage { get; set; }
    public string? LogMessage { get; set; }
    
    public T? Data { get; set; }
}