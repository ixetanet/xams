using System.Text.Json;

namespace Xams.Core.Dtos.Data
{
    public class ActionInput
    {
        public string name { get; set; }
        public Dictionary<string, JsonElement>? parameters { get; set; }
    }
}