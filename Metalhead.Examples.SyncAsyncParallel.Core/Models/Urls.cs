using System.Text.Json.Serialization;

namespace Metalhead.Examples.SyncAsyncParallel.Core.Models;

public class UrlsToDownload
{
    [JsonPropertyName("urls")]
    public List<string> Urls { get; set; } = [];
}
