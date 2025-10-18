using Newtonsoft.Json;

namespace Watermark.Core;

public sealed class Template
{
    [JsonProperty("schemaVersion")] public string SchemaVersion { get; set; } = "1.0";
    [JsonProperty("canvas")] public CanvasSpec Canvas { get; set; } = new();
    [JsonProperty("variables")] public Dictionary<string, object>? Variables { get; set; } = new();
    [JsonProperty("layers")] public List<LayerBase> Layers { get; set; } = new();
    [JsonProperty("export")] public ExportSettings Export { get; set; } = new();
}
