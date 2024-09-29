using System.Text.Json;

namespace Xams.Core.Dtos.Data;

public class PermissionsInput
{
    public string method { get; set; }
    public Dictionary<string, JsonElement>? parameters { get; set; }
}