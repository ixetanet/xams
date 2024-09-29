using System.Text.Json;

namespace Xams.Core.Dtos.Data
{
    public class Input
    {
        public string? tableName { get; set; } // Could be null in the case of a batch operation
        public Dictionary<string,dynamic>? fields { get; set; } // Could be null in the case of a batch operation
        public Dictionary<string, JsonElement>? parameters { get; set; }
    }
}