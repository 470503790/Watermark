using Newtonsoft.Json;

namespace Watermark.Core;

public abstract class LayerBase
{
    [JsonProperty("type")] public abstract string Type { get; }
    [JsonProperty("id")] public string Id { get; set; } = Guid.NewGuid().ToString("N");
    [JsonProperty("name")] public string Name { get; set; } = "";
    [JsonProperty("visible")] public bool Visible { get; set; } = true;
    [JsonProperty("visibleExpr")] public string? VisibleExpr { get; set; }
    [JsonProperty("opacity")] public float Opacity { get; set; } = 1f;
    [JsonProperty("blendMode")] public BlendMode BlendMode { get; set; } = BlendMode.Normal;
    [JsonProperty("transform")] public TransformSpec Transform { get; set; } = new();
    [JsonProperty("tiling")] public TilingSpec? Tiling { get; set; }
}
