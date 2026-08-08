# CoreWcf.ConfigTransformer

`CoreWcf.ConfigTransformer` helps migrate legacy WCF `<system.serviceModel>` XML into configuration that is easier to consume from a CoreWCF-hosted service.

The transformer is intentionally focused. It does not claim to prove whether every WCF configuration element is supported by CoreWCF. It applies a curated set of transformations, derives listener addresses, and reports diagnostics so the remaining migration work is visible.

## What It Does

- Transforms a `<system.serviceModel>` element without mutating the caller's original `XElement`.
- Removes configuration that this transformer does not currently preserve or map when `RemoveUnsupportedConfiguration` is enabled.
- Resolves relative service endpoint addresses from matching host base addresses.
- Derives HTTP, HTTPS, and Net.TCP listener base addresses.
- Generates missing service and endpoint names where CoreWCF-style hosting needs stable identifiers.
- Emits diagnostics for lossy, generated, changed, or invalid transformation decisions.

## What It Does Not Do

- It does not convert service implementation code.
- It does not generate CoreWCF `Program.cs` hosting code.
- It does not provide a complete CoreWCF support matrix.
- It does not transform WCF client configuration.
- It does not guarantee that preserved XML is fully supported by the CoreWCF packages used by your application.

## Quick Start

Transform a full configuration file and preserve unrelated sections by default:

```csharp
using CoreWcf.ConfigTransformer;

var transformer = new LegacyWcfServiceModelToCoreWcfTransformer();
var result = transformer.Transform(
    "legacy.app.config",
    "generated.app.config");

foreach (var listener in result.HttpListeners)
{
    Console.WriteLine(listener);
}

foreach (var diagnostic in result.Diagnostics)
{
    Console.WriteLine(diagnostic);
}
```

By default, the file overload keeps the original configuration document and replaces only its `<system.serviceModel>` section. Sections such as `<appSettings>` and `<connectionStrings>` are preserved.

To write a file containing only the transformed `<system.serviceModel>` section:

```csharp
var result = transformer.Transform(
    "legacy.app.config",
    "generated.serviceModel.config",
    new LegacyWcfServiceModelTransformOptions
    {
        GeneratedConfigurationMode = GeneratedConfigurationMode.ServiceModelOnly
    });
```

Transform an in-memory `<system.serviceModel>` element:

```csharp
using System.Xml.Linq;
using CoreWcf.ConfigTransformer;

var serviceModel = XElement.Parse("""
<system.serviceModel>
  <services>
    <service name="Sample.Service">
      <host>
        <baseAddresses>
          <add baseAddress="http://localhost:8080/Service" />
        </baseAddresses>
      </host>
      <endpoint address="" binding="basicHttpBinding" contract="Sample.IService" />
    </service>
  </services>
</system.serviceModel>
""");

var transformer = new LegacyWcfServiceModelToCoreWcfTransformer();
var result = transformer.Transform(serviceModel);
```

## Options

```csharp
var options = new LegacyWcfServiceModelTransformOptions
{
    RemoveUnsupportedConfiguration = true,
    ThrowOnError = false,
    GeneratedConfigurationMode = GeneratedConfigurationMode.ReplaceServiceModelInConfiguration
};
```

- `RemoveUnsupportedConfiguration`: removes configuration this transformer does not currently preserve or map. Defaults to `true`.
- `ThrowOnError`: throws `LegacyWcfServiceModelTransformException` if error diagnostics are produced. Defaults to `false`.
- `GeneratedConfigurationMode`: controls how the path-based overload writes the output file. Defaults to `ReplaceServiceModelInConfiguration`.

## Documentation

- `docs/usage.md`: API usage patterns and output modes.
- `docs/transformation-behavior.md`: detailed transformation behavior and diagnostics.
- `docs/scope-and-limitations.md`: project scope, support assumptions, and review guidance.

## Migration Guidance

Treat the generated configuration as a migration aid, not as the final authority. Review all diagnostics, verify preserved configuration against the CoreWCF packages and version used by your application, and add manual CoreWCF hosting code as needed.
