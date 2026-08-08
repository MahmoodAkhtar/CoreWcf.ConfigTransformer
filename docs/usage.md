# Usage

This library exposes a single main entry point: `LegacyWcfServiceModelToCoreWcfTransformer`.

Use the path-based overload when you want to transform a legacy `.config` file. Use the `XElement` overload when another tool already extracted `<system.serviceModel>` for you.

## Transform A Configuration File

```csharp
using CoreWcf.ConfigTransformer;

var transformer = new LegacyWcfServiceModelToCoreWcfTransformer();
var result = transformer.Transform(
    "legacy.app.config",
    "generated.app.config");
```

The default output mode is `GeneratedConfigurationMode.ReplaceServiceModelInConfiguration`. The generated file is a copy of the original configuration file with only `<system.serviceModel>` replaced by the transformed section.

This default is intentionally conservative for full-file input. If the source file contains `<appSettings>`, `<connectionStrings>`, custom sections, or other unrelated configuration, those sections are preserved.

## Write Only system.serviceModel

Use `GeneratedConfigurationMode.ServiceModelOnly` when the output should be a small generated configuration document containing only the transformed service model section.

```csharp
var result = transformer.Transform(
    "legacy.app.config",
    "generated.serviceModel.config",
    new LegacyWcfServiceModelTransformOptions
    {
        GeneratedConfigurationMode = GeneratedConfigurationMode.ServiceModelOnly
    });
```

The output file shape is:

```xml
<configuration>
  <system.serviceModel>
    ...
  </system.serviceModel>
</configuration>
```

## Transform An XElement

```csharp
using System.Xml.Linq;
using CoreWcf.ConfigTransformer;

var serviceModel = XDocument
    .Load("legacy.app.config", LoadOptions.PreserveWhitespace)
    .Root?
    .Element("system.serviceModel");

if (serviceModel is null)
{
    throw new InvalidOperationException("Missing system.serviceModel.");
}

var transformer = new LegacyWcfServiceModelToCoreWcfTransformer();
var result = transformer.Transform(serviceModel);
```

The transformer clones the input element before applying rules. The caller's original `XElement` is not mutated.

## Handle Diagnostics

Diagnostics are the primary review mechanism. They describe generated names, resolved addresses, removed configuration, unsupported bindings, and errors.

```csharp
foreach (var diagnostic in result.Diagnostics)
{
    Console.WriteLine(diagnostic);
}

if (result.HasErrors)
{
    // Stop migration automation or require manual review.
}
```

To throw when error diagnostics are produced:

```csharp
var result = transformer.Transform(
    "legacy.app.config",
    "generated.app.config",
    new LegacyWcfServiceModelTransformOptions
    {
        ThrowOnError = true
    });
```

## Use Derived Listener Addresses

The result separates derived listeners by transport:

```csharp
foreach (var listener in result.HttpListeners)
{
    Console.WriteLine($"HTTP: {listener}");
}

foreach (var listener in result.HttpsListeners)
{
    Console.WriteLine($"HTTPS: {listener}");
}

foreach (var listener in result.NetTcpListeners)
{
    Console.WriteLine($"Net.TCP: {listener}");
}
```

These values are intended to help configure the CoreWCF host. The transformer does not generate hosting code.
