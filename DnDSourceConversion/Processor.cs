using System.Diagnostics;
using System.Dynamic;
using System.Text.Json.Nodes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using YamlDotNet.Serialization;

namespace DnDSourceConversion;

public class Processor
{
    private const string INPUT_FILE_PATH = @"C:/Users/brage/OneDrive/Obsidian/Vaults/D&D/";
    private const bool USE_TEST_FILE = false;
    private const string TEST_FILE_NAME = "test-data";
    private const string INPUT_FILE_EXTENSION = ".json";

    public static void Process(IConfig config)
    {
        var sw = Stopwatch.StartNew();
        string? json = JsonUtils.GetJson(INPUT_FILE_PATH + (USE_TEST_FILE ? TEST_FILE_NAME : config.InputFileName) +
                                         INPUT_FILE_EXTENSION);

        if (json is null)
        {
            Console.WriteLine("Failed to Parse Json. Could be empty.");
            return;
        }

        json = JsonUtils.PrepareJsonForDynamicTyping(json);
        JsonNode? rootNode = JsonNode.Parse(json);
        rootNode = JsonUtils.FixJsonNode(rootNode);

        JsonArray? array = rootNode.AsArray(); // Here i'm assuming that the root node is an array.

        ISerializer? yamlSerializer = new Serializer();

        for (int i = 0; i < array.Count; i++)
        {
            JsonNode? childNode = array[i];
            JsonObject? objectNode = childNode.AsObject(); // Here i'm assuming that the child node is an object.

            string name = config.FileNameProvider.GetFileName(objectNode, i.ToString());
            config.Adjustments.Adjust(objectNode, name);
            string frontMatterJson = objectNode.ToJsonString();

            var converter = new ExpandoObjectConverter();
            dynamic frontMatter = JsonConvert.DeserializeObject<ExpandoObject>(frontMatterJson, converter);

            string frontMatterYaml = yamlSerializer.Serialize(frontMatter);
            frontMatterYaml = frontMatterYaml.TrimEnd();

            config.Adjustments.AdjustStatblock(objectNode, name);
            string statBlockJson = objectNode.ToJsonString();
            dynamic statblock = JsonConvert.DeserializeObject<ExpandoObject>(statBlockJson, converter);

            string statblockYaml = yamlSerializer.Serialize(statblock);
            statblockYaml = statblockYaml.TrimEnd();

            statblockYaml = config.Adjustments.HandleReplacements(statblockYaml, name);

            string md = config.MarkdownGenerator.Generate(frontMatterYaml, statblockYaml);

            if (GlobalConfig.DRY_RUN)
                continue;

            Write(config, name, md);
            Console.WriteLine($"{name} done.");
        }
        
        sw.Stop();
        Console.WriteLine($"{config.GetType().Name.Replace("Config", "")} done in {sw.ElapsedMilliseconds} ms.");
    }

    public static async Task ProcessAsync(IConfig config)
    {
        var sw = Stopwatch.StartNew();
        string? json = JsonUtils.GetJson(INPUT_FILE_PATH + (USE_TEST_FILE ? TEST_FILE_NAME : config.InputFileName) +
                                         INPUT_FILE_EXTENSION);

        if (json is null)
        {
            Console.WriteLine("Failed to Parse Json. Could be empty.");
            return;
        }

        json = JsonUtils.PrepareJsonForDynamicTyping(json);
        JsonNode? rootNode = JsonNode.Parse(json);
        rootNode = JsonUtils.FixJsonNode(rootNode);

        JsonArray? array = rootNode.AsArray(); // Here i'm assuming that the root node is an array.

        ISerializer? yamlSerializer = new Serializer();

        List<Task> tasks = new(array.Count);

        for (int i = 0; i < array.Count; i++)
        {
            JsonNode? childNode = array[i];
            JsonObject? objectNode = childNode.AsObject(); // Here i'm assuming that the child node is an object.

            string name = config.FileNameProvider.GetFileName(objectNode, i.ToString());
            config.Adjustments.Adjust(objectNode, name);
            string frontMatterJson = objectNode.ToJsonString();

            var converter = new ExpandoObjectConverter();
            dynamic frontMatter = JsonConvert.DeserializeObject<ExpandoObject>(frontMatterJson, converter);

            string frontMatterYaml = yamlSerializer.Serialize(frontMatter);
            frontMatterYaml = frontMatterYaml.TrimEnd();

            config.Adjustments.AdjustStatblock(objectNode, name);
            string statBlockJson = objectNode.ToJsonString();
            dynamic statblock = JsonConvert.DeserializeObject<ExpandoObject>(statBlockJson, converter);

            string statblockYaml = yamlSerializer.Serialize(statblock);
            statblockYaml = statblockYaml.TrimEnd();

            statblockYaml = config.Adjustments.HandleReplacements(statblockYaml, name);

            string md = config.MarkdownGenerator.Generate(frontMatterYaml, statblockYaml);

            if (GlobalConfig.DRY_RUN)
                continue;

            Task? task = WriteAsync(config, name, md, config.OutputFileExtension);

            tasks.Add(task);
            tasks.Add(task.ContinueWith(_ => Console.WriteLine($"{name} done.")));
        }

        await Task.WhenAll(tasks);
        
        sw.Stop();
        Console.WriteLine($"{config.GetType().Name.Replace("Config", "")} done in {sw.ElapsedMilliseconds} ms.");
    }
    
    private static void Write(IConfig config, string name, string yaml)
    {
        if (File.Exists(config.OutputFilePath + name + config.OutputFileExtension))
            File.Delete(config.OutputFilePath + name + config.OutputFileExtension);

        using var fileStream = new FileStream(config.OutputFilePath + name + config.OutputFileExtension, FileMode.OpenOrCreate);

        using var writer = new StreamWriter(fileStream);
        writer.Write(yaml);
    }
    
    private static async Task? WriteAsync(IConfig config, string name, string yaml, string fileExtension)
    {
        if (File.Exists(config.OutputFilePath + name + fileExtension))
            File.Delete(config.OutputFilePath + name + fileExtension);

        await using var fileStream =
            new FileStream(config.OutputFilePath + name + fileExtension, FileMode.OpenOrCreate);

        await using var writer = new StreamWriter(fileStream);
        await writer.WriteAsync(yaml);
    }
}