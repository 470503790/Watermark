using Newtonsoft.Json;

namespace Watermark.Core;

public sealed class TextStyle
{
    [JsonProperty("fontFamily")] public string FontFamily { get; set; } = "Arial";
    [JsonProperty("fontSize")] public float FontSize { get; set; } = 24f;
    [JsonProperty("fontWeight")] public string FontWeight { get; set; } = "normal"; // normal|bold
    [JsonProperty("fontStyle")] public string FontStyle { get; set; } = "normal";   // normal|italic
    [JsonProperty("fillColor")] public string FillColor { get; set; } = "#FFFFFFFF";
    [JsonProperty("textAlign")] public string TextAlign { get; set; } = "left";
    [JsonProperty("maxWidth")] public int? MaxWidth { get; set; }
}
