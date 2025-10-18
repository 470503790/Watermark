using Newtonsoft.Json;
using Watermark.Core;

namespace Watermark.IO;

public static class TemplateService
{
    public static void Save(string path, Template template)
    {
        var json = JsonConvert.SerializeObject(template, Formatting.Indented);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, json);
    }

    public static Template Load(string path)
    {
        var json = File.ReadAllText(path);
        var settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto,
            SerializationBinder = new Newtonsoft.Json.Serialization.DefaultSerializationBinder()
        };
        return JsonConvert.DeserializeObject<Template>(json, settings) ?? new Template();
    }
}
