namespace Xams.Core.Utils;

public class ServiceException : Exception
{
    public string friendlyMessage { get; }
    public string logMessage { get; }

    public ServiceException(string friendlyMessage, string logMessage)
    {
        this.friendlyMessage = friendlyMessage;
        this.logMessage = logMessage;
    }
}