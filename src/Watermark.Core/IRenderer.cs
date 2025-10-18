namespace Watermark.Core;

public interface IRenderer
{
    void RenderToFile(Template template, string inputImagePath, string outputPath, ExportSettings? overrideSettings = null);
}
