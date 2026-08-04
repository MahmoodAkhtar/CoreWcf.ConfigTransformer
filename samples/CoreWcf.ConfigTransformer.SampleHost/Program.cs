using System.Xml.Linq;
using CoreWcf.ConfigTransformer;

var configurationPath = Path.Combine(AppContext.BaseDirectory, "app.config");
var configuration = XDocument.Load(configurationPath, LoadOptions.PreserveWhitespace);
var serviceModel = configuration.Root?.Element("system.serviceModel");
if (serviceModel is null)
{
    throw new InvalidOperationException("The configuration does not contain a system.serviceModel section.");
}

var transformer = new LegacyWcfServiceModelToCoreWcfTransformer();
var result = transformer.Transform(serviceModel);

Console.WriteLine("HTTP listeners:");
foreach (var listener in result.HttpListeners)
{
    Console.WriteLine($"- {listener}");
}

Console.WriteLine("Net.TCP listeners:");
foreach (var listener in result.NetTcpListeners)
{
    Console.WriteLine($"- {listener}");
}

if (result.Diagnostics.Count > 0)
{
    Console.WriteLine("Diagnostics:");
    foreach (var diagnostic in result.Diagnostics)
    {
        Console.WriteLine($"- {diagnostic}");
    }
}
