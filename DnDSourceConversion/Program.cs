using System.Diagnostics;
using System.Dynamic;
using System.Text.Json.Nodes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using YamlDotNet.Serialization;
using DnDSourceConversion;;

const bool PROCESS_ALL = true;

var sw = Stopwatch.StartNew();

if (PROCESS_ALL)
{
    // Creates an instance of all classes that implements IConfig.
    List<IConfig> configs = AppDomain.CurrentDomain
        .GetAssemblies()
        .SelectMany(a => a.GetTypes())
        .Where(t => !t.IsAbstract && t.GetInterfaces().Contains(typeof(IConfig)))
        .Select(t => Activator.CreateInstance(t))
        .Cast<IConfig>()
        .ToList();

    foreach (IConfig config in configs)
    {
        if (GlobalConfig.ASYNC)
            Processor.ProcessAsync(config);
        else
            Processor.Process(config);
    }
}
else
{
    IConfig config = new SpellConfig();
    
    if (GlobalConfig.ASYNC)
        Processor.ProcessAsync(config);
    else
        Processor.Process(config);
}

sw.Stop();
Console.WriteLine($"All done. {sw.ElapsedMilliseconds}");

public static class GlobalConfig
{
    public const bool ASYNC = false;
    public const bool DRY_RUN = false; // When ture, no files will be written to.
    public const bool FIND_IMAGES = false;
}
