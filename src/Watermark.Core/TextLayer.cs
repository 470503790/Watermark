using Newtonsoft.Json;

namespace Watermark.Core;

public sealed class TextLayer : LayerBase
{
    public override string Type => nameof(TextLayer);
    [JsonProperty("text")] public string Text { get; set; } = "Text";
    [JsonProperty("style")] public TextStyle Style { get; set; } = new();
}
