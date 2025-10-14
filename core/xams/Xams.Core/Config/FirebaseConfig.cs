namespace Xams.Core.Config;

public class FirebaseConfig
{
    public required string apiKey { get; set; }
    public required string authDomain { get; set; }
    public required string projectId { get; set; }
    public required string storageBucket { get; set; }
    public required string messagingSenderId { get; set; }
    public required string appId { get; set; }
    public required string measurementId { get; set; }
    public required string[] providers { get; set; } = [];
    public bool enableSmsMfa { get; set; } = false;
}