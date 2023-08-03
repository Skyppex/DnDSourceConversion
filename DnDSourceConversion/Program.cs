// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using System.Linq;

Console.WriteLine("Hello, World!");

const string INPUT_FILE_PATH = @"C:/Users/brage/OneDrive/Obsidian/Vaults/D&D/";
const string INPUT_FILE_NAME = "bestiary-sublist-data";
const string INPUT_FILE_EXTENSION = ".json";

const string OUTPUT_FILE_PATH = @"C:/Users/brage/OneDrive/Obsidian/Vaults/D&D/Bestiary/";
const string OUTPUT_FILE_NAME = "bestiary-sublist-data";
const string OUTPUT_FILE_EXTENSION = ".yaml";

using var stream = new FileStream(INPUT_FILE_PATH + INPUT_FILE_NAME + INPUT_FILE_EXTENSION, FileMode.Open);

using var reader = new StreamReader(stream);
var json = reader.ReadToEnd();

// string json = SampleJson();

if (json is null)
{
    Console.WriteLine("Failed to Parse Json. Seems to be empty.");
    return;
}

json = json.TrimStart();

if (json[0] != '{')
    json = "{\"head\":" + json + '}';

// json = json.Replace('\'', '\"');

var rootNode = JsonNode.Parse(json);

if (rootNode!.AsObject().TryGetPropertyValue("head", out JsonNode? headNode))
    rootNode = headNode;

var array = rootNode.AsArray();
for (var i = 0; i < array.Count; i++)
{
    var childNode = array[(Index)i];

    var objectNode = childNode.AsObject();

    List<string> invalidNames = objectNode.Where(kvp => kvp.Key.StartsWith('_'))
        .Select(kvp => kvp.Key)
        .ToList();

    invalidNames.ForEach(name => objectNode.Remove(name));
    
    string childJson = objectNode.ToJsonString();

    string name = i.ToString();
    if (!objectNode.AsObject().TryGetPropertyValue("name", out JsonNode nameNode))
        Console.WriteLine($"JsonObject at index: {i} has no name property.");
    else
        name = nameNode!.AsValue().GetValue<string>();
    
    var converter = new ExpandoObjectConverter();
    dynamic obj = JsonConvert.DeserializeObject<ExpandoObject>(childJson, converter);

    var yamlSerializer = new Serializer();
    string yaml = yamlSerializer.Serialize(obj);

    File.WriteAllText(OUTPUT_FILE_PATH + name + OUTPUT_FILE_EXTENSION, yaml);
}

static string SampleJson()
{
    return  """
            {
                "head":[
                    {
                        "Name":"Peter",
                        "Age":22,
                        "CourseDet": {
                            "CourseName":"CS",
                            "CourseDescription":"Computer Science"
                        },
                        "Subjects":[
                            "Computer Languages",
                            "Operating Systems"
                        ]
                    },
                    {
                        "Name":"Mark",
                        "Age":34,
                        "CourseDet": {
                            "CourseName":"IT",
                            "CourseDescription":"Information Technology
                        },
                        "Subjects":[
                            "Databases",
                            "Web Technologies",
                            "Programming"
                        ]
                    },
                    {
                        "Name":"Sam",
                        "Age":43,
                        "CourseDet": {
                            "CourseName":"CC",
                            "CourseDescription":"Cloud Computing"
                        },
                        "Subjects":[
                            "Cloud Architecture",
                            "Cloud Security",
                            "Cloud Services",
                            "Cloud Programming"
                        ]
                    }
                ]
            }
            """;
}

