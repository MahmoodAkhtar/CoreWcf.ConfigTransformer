using System.Xml.Linq;
using CoreWcf.ConfigTransformer.Internal;

namespace CoreWcf.ConfigTransformer;

/// <summary>
/// Transforms legacy WCF service model configuration into a CoreWCF-friendly service model.
/// </summary>
public sealed class LegacyWcfServiceModelToCoreWcfTransformer
{
    private static readonly ISet<string> SupportedServiceModelSections = new HashSet<string>(StringComparer.Ordinal)
    {
        "bindings",
        "services"
    };

    private static readonly ISet<string> SupportedBindings = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "basicHttpBinding",
        "netTcpBinding",
        "webHttpBinding",
        "wsHttpBinding"
    };

    /// <summary>
    /// Transforms the specified system.serviceModel configuration element.
    /// </summary>
    public LegacyWcfServiceModelTransformResult Transform(XElement serviceModel, LegacyWcfServiceModelTransformOptions options = null)
    {
        Guard.NotNull(serviceModel, nameof(serviceModel));

        var context = new TransformationContext(options ?? new LegacyWcfServiceModelTransformOptions());
        var state = new TransformationState(new XElement(serviceModel));

        TransformServiceModel(context, state);
        return Complete(context, state);
    }

    /// <summary>
    /// Transforms the system.serviceModel element from a legacy configuration file and writes the transformed element to a generated configuration file.
    /// </summary>
    public LegacyWcfServiceModelTransformResult Transform(
        string legacyConfigurationPath,
        string generatedConfigurationPath,
        LegacyWcfServiceModelTransformOptions options)
    {
        Guard.NotNullOrWhiteSpace(legacyConfigurationPath, nameof(legacyConfigurationPath));
        Guard.NotNullOrWhiteSpace(generatedConfigurationPath, nameof(generatedConfigurationPath));

        if (!File.Exists(legacyConfigurationPath))
        {
            throw new FileNotFoundException("The legacy configuration file could not be found.", legacyConfigurationPath);
        }

        var generatedConfigurationDirectory = Path.GetDirectoryName(Path.GetFullPath(generatedConfigurationPath));
        if (!string.IsNullOrWhiteSpace(generatedConfigurationDirectory) && !Directory.Exists(generatedConfigurationDirectory))
        {
            throw new DirectoryNotFoundException($"The generated configuration directory could not be found: '{generatedConfigurationDirectory}'.");
        }

        var legacyConfiguration = XDocument.Load(legacyConfigurationPath, LoadOptions.PreserveWhitespace);
        var serviceModel = legacyConfiguration.Root?.Element("system.serviceModel");
        if (serviceModel is null)
        {
            throw new LegacyWcfServiceModelTransformException("The legacy configuration does not contain a system.serviceModel section.");
        }

        var result = Transform(serviceModel, options);
        File.WriteAllText(generatedConfigurationPath, result.ServiceModel.ToString());

        return result;
    }

    private static void TransformServiceModel(TransformationContext context, TransformationState state)
    {
        var serviceModel = state.ServiceModel;

        RemoveUnsupportedSections(context, state);
        RemoveUnsupportedBindings(context, state);
        RemoveUnsupportedBindingElements(context, state);
        EnsureUniqueServiceNames(state);
        RemoveUnsupportedEndpoints(context, state);

        var bindingIndex = BindingConfigurationIndex.Create(serviceModel);
        foreach (var service in serviceModel.Element("services")?.Elements("service") ?? Enumerable.Empty<XElement>())
        {
            var baseAddresses = ReadBaseAddresses(state, service);
            foreach (var endpoint in service.Elements("endpoint"))
            {
                ProcessEndpoint(state, bindingIndex, service, endpoint, baseAddresses);
            }

            service.Element("host")?.Remove();
        }
    }

    private static void EnsureUniqueServiceNames(TransformationState state)
    {
        var usedNames = new HashSet<string>(StringComparer.Ordinal);
        var services = state.ServiceModel.Element("services")?.Elements("service").ToArray() ?? Array.Empty<XElement>();

        for (var index = 0; index < services.Length; index++)
        {
            var service = services[index];
            var candidate = ((string)service.Attribute("name"))?.Trim();
            if (string.IsNullOrWhiteSpace(candidate))
            {
                candidate = ((string)service.Elements("endpoint").FirstOrDefault()?.Attribute("contract"))?.Trim();
            }

            if (string.IsNullOrWhiteSpace(candidate))
            {
                candidate = $"Service{index + 1}";
            }

            service.SetAttributeValue("name", CreateUniqueServiceName(candidate, usedNames));
        }
    }

    private static string CreateUniqueServiceName(string candidate, ISet<string> usedNames)
    {
        if (usedNames.Add(candidate))
        {
            return candidate;
        }

        for (var suffix = 2;; suffix++)
        {
            var uniqueName = $"{candidate}_{suffix}";
            if (usedNames.Add(uniqueName))
            {
                return uniqueName;
            }
        }
    }

    private static void RemoveUnsupportedSections(TransformationContext context, TransformationState state)
    {
        if (!context.Options.RemoveUnsupportedConfiguration)
        {
            return;
        }

        state.ServiceModel
            .Elements()
            .Where(element => !SupportedServiceModelSections.Contains(element.Name.LocalName))
            .Remove();
    }

    private static void RemoveUnsupportedBindings(TransformationContext context, TransformationState state)
    {
        if (!context.Options.RemoveUnsupportedConfiguration)
        {
            return;
        }

        var bindings = state.ServiceModel.Element("bindings")?.Elements().ToArray() ?? Array.Empty<XElement>();
        foreach (var binding in bindings)
        {
            if (!SupportedBindings.Contains(binding.Name.LocalName))
            {
                binding.Remove();
            }
        }
    }

    private static void RemoveUnsupportedBindingElements(TransformationContext context, TransformationState state)
    {
        if (!context.Options.RemoveUnsupportedConfiguration)
        {
            return;
        }

        state.ServiceModel
            .Element("bindings")?
            .Elements()
            .Elements("binding")
            .Elements("reliableSession")
            .Remove();
    }

    private static void RemoveUnsupportedEndpoints(TransformationContext context, TransformationState state)
    {
        if (!context.Options.RemoveUnsupportedConfiguration)
        {
            return;
        }

        var endpoints = state.ServiceModel.Element("services")?.Elements("service").Elements("endpoint").ToArray() ?? Array.Empty<XElement>();
        foreach (var endpoint in endpoints)
        {
            var binding = ((string)endpoint.Attribute("binding"))?.Trim();
            if (!SupportedBindings.Contains(binding))
            {
                state.AddDiagnostic(
                    LegacyWcfServiceModelDiagnosticSeverity.Warning,
                    LegacyWcfServiceModelDiagnosticCodes.UnsupportedBinding,
                    $"Endpoint '{(string)endpoint.Attribute("contract")}' uses unsupported binding '{binding}'.");
                endpoint.Remove();
            }
        }
    }

    private static IReadOnlyList<Uri> ReadBaseAddresses(TransformationState state, XElement service)
    {
        var uris = new List<Uri>();
        var addressElements = service
            .Element("host")?
            .Element("baseAddresses")?
            .Elements("add") ?? Enumerable.Empty<XElement>();

        foreach (var add in addressElements)
        {
            var value = ((string)add.Attribute("baseAddress"))?.Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
            {
                state.AddDiagnostic(
                    LegacyWcfServiceModelDiagnosticSeverity.Error,
                    LegacyWcfServiceModelDiagnosticCodes.InvalidBaseAddress,
                    $"Service '{(string)service.Attribute("name")}' has invalid base address '{value}'.");
                continue;
            }

            uris.Add(uri);
        }

        return uris;
    }

    private static void ProcessEndpoint(TransformationState state, BindingConfigurationIndex bindingIndex, XElement service, XElement endpoint, IReadOnlyList<Uri> baseAddresses)
    {
        var binding = ((string)endpoint.Attribute("binding"))?.Trim();
        var bindingConfiguration = ((string)endpoint.Attribute("bindingConfiguration"))?.Trim();
        var endpointAddress = ((string)endpoint.Attribute("address"))?.Trim();
        var bindingElement = bindingIndex.Find(binding, bindingConfiguration);
        var transport = ResolveTransport(binding, bindingElement);

        if (transport == EndpointTransport.Unknown)
        {
            return;
        }

        var endpointUri = ResolveEndpointAddress(state, service, transport, endpointAddress, baseAddresses);
        if (endpointUri is not null)
        {
            endpoint.SetAttributeValue("address", endpointUri.ToString());
            state.AddListener(transport, endpointUri);
        }
    }

    private static EndpointTransport ResolveTransport(string binding, XElement bindingElement)
    {
        if (string.Equals(binding, "netTcpBinding", StringComparison.OrdinalIgnoreCase))
        {
            return EndpointTransport.NetTcp;
        }

        if (string.Equals(binding, "basicHttpBinding", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(binding, "wsHttpBinding", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(binding, "webHttpBinding", StringComparison.OrdinalIgnoreCase))
        {
            return HasHttpsSecurity(bindingElement) ? EndpointTransport.Https : EndpointTransport.Http;
        }

        return EndpointTransport.Unknown;
    }

    private static bool HasHttpsSecurity(XElement bindingElement)
    {
        var mode = (string)bindingElement?.Element("security")?.Attribute("mode");
        return string.Equals(mode, "Transport", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(mode, "TransportWithMessageCredential", StringComparison.OrdinalIgnoreCase);
    }

    private static Uri ResolveEndpointAddress(TransformationState state, XElement service, EndpointTransport transport, string endpointAddress, IReadOnlyList<Uri> baseAddresses)
    {
        if (Uri.TryCreate(endpointAddress, UriKind.Absolute, out var absoluteEndpoint))
        {
            return absoluteEndpoint;
        }

        var scheme = transport == EndpointTransport.NetTcp ? "net.tcp" : transport.ToString().ToLowerInvariant();
        var baseAddress = baseAddresses.FirstOrDefault(uri => string.Equals(uri.Scheme, scheme, StringComparison.OrdinalIgnoreCase));
        if (baseAddress is null)
        {
            state.AddDiagnostic(
                LegacyWcfServiceModelDiagnosticSeverity.Error,
                LegacyWcfServiceModelDiagnosticCodes.MissingBaseAddress,
                $"Service '{(string)service.Attribute("name")}' has no '{scheme}' base address for endpoint '{endpointAddress}'.");
            return null;
        }

        if (string.IsNullOrWhiteSpace(endpointAddress))
        {
            return baseAddress;
        }

        return new Uri(baseAddress, endpointAddress);
    }

    private static LegacyWcfServiceModelTransformResult Complete(TransformationContext context, TransformationState state)
    {
        var diagnostics = state.Diagnostics;
        if (context.Options.ThrowOnError && diagnostics.Any(diagnostic => diagnostic.Severity == LegacyWcfServiceModelDiagnosticSeverity.Error))
        {
            throw new LegacyWcfServiceModelTransformException("The configuration could not be transformed because one or more errors were reported.");
        }

        return new LegacyWcfServiceModelTransformResult(
            state.ServiceModel,
            state.HttpListeners,
            state.HttpsListeners,
            state.NetTcpListeners,
            diagnostics);
    }
}
