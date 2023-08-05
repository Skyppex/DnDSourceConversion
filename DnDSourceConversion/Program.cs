using System.Dynamic;
using System.Text.Json.Nodes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using YamlDotNet.Serialization;
using DnDSourceConversion;

const string INPUT_FILE_PATH = @"C:/Users/brage/OneDrive/Obsidian/Vaults/D&D/";
const string INPUT_FILE_NAME = "bestiary-sublist-data";
const string INPUT_FILE_EXTENSION = ".json";

const string OUTPUT_FILE_PATH = @"C:/Users/brage/OneDrive/Obsidian/Vaults/D&D/_Source/Official/Bestiary/";
const string OUTPUT_FILE_EXTENSION = ".md";

string? json = JsonUtils.GetJson(INPUT_FILE_PATH + INPUT_FILE_NAME + INPUT_FILE_EXTENSION);

if (json is null)
{
    Console.WriteLine("Failed to Parse Json. Seems to be empty.");
    return;
}

json = JsonUtils.PrepareJsonForDynamicTyping(json);
JsonNode? rootNode = JsonNode.Parse(json);
rootNode = JsonUtils.FixJsonNode(rootNode);

JsonArray? array = rootNode.AsArray(); // Here i'm assuming that the root node is an array.

var yamlSerializer = new Serializer();
// List<Task> tasks = new(array.Count);

var config = new MonsterConfig();

for (int i = 0; i < array.Count; i++)
{
    JsonNode? childNode = array[(Index)i];
    JsonObject? objectNode = childNode.AsObject(); // Here i'm assuming that the child node is an object.

    string name = config.FileNameProvider.GetFileName(objectNode, i.ToString());
    config.Adjustments.Adjust(objectNode);
    string frontMatterJson = objectNode.ToJsonString();

    var converter = new ExpandoObjectConverter();
    dynamic frontMatter = JsonConvert.DeserializeObject<ExpandoObject>(frontMatterJson, converter);
    
    string frontMatterYaml = yamlSerializer.Serialize(frontMatter);
    frontMatterYaml = frontMatterYaml.TrimEnd();
    frontMatterYaml = config.Adjustments.HandleReplacements(frontMatterYaml);

    config.Adjustments.AdjustStatblock(objectNode);
    string statBlockJson = objectNode.ToJsonString();
    dynamic statblock = JsonConvert.DeserializeObject<ExpandoObject>(statBlockJson, converter);

    string statblockYaml = yamlSerializer.Serialize(statblock);
    statblockYaml = statblockYaml.TrimEnd();

    statblockYaml = config.Adjustments.HandleReplacements(statblockYaml);
    
    string md = config.MarkdownGenerator.Generate(frontMatterYaml, statblockYaml);
    
    Write(name, md, OUTPUT_FILE_EXTENSION);
    
    // Task? task = WriteAsync(name, md, OUTPUT_FILE_EXTENSION);
    //
    // tasks.Add(task);
    // tasks.Add(task.ContinueWith(_ => Console.WriteLine(name + " done.")));
}

// await Task.WhenAll(tasks);
Console.WriteLine("All done.");

async Task? WriteAsync(string name, string yaml, string fileExtension)
{
    await using var fileStream = new FileStream(OUTPUT_FILE_PATH + name + fileExtension, FileMode.OpenOrCreate);
                                  
    await using var writer = new StreamWriter(fileStream);
    await writer.WriteAsync(yaml);
}

void Write(string name, string yaml, string fileExtension)
{
    using var fileStream = new FileStream(OUTPUT_FILE_PATH + name + fileExtension, FileMode.OpenOrCreate);
                                  
    using var writer = new StreamWriter(fileStream);
    writer.Write(yaml);
}
