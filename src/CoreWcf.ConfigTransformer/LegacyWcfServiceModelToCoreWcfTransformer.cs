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
        LegacyWcfServiceModelTransformOptions options = null)
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
        var generatedConfiguration = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("configuration", result.ServiceModel));

        generatedConfiguration.Save(generatedConfigurationPath);

        return result;
    }

    private static void TransformServiceModel(TransformationContext context, TransformationState state)
    {
        var serviceModel = state.ServiceModel;

        RemoveUnsupportedSections(context, state);
        RemoveUnsupportedBindings(context, state);
        RemoveUnsupportedBindingElements(context, state);
        RemoveUnsupportedBindingAttributes(context, state);
        EnsureUniqueServiceNames(state);
        RemoveUnsupportedEndpoints(context, state);
        EnsureUniqueEndpointNames(state);

        var bindingIndex = BindingConfigurationIndex.Create(serviceModel);
        foreach (var service in serviceModel.Element("services")?.Elements("service") ?? Enumerable.Empty<XElement>())
        {
            var baseAddresses = ReadBaseAddresses(state, service);
            foreach (var endpoint in service.Elements("endpoint"))
            {
                ProcessEndpoint(state, bindingIndex, service, endpoint, baseAddresses);
            }

            var host = service.Element("host");
            if (host is not null)
            {
                state.AddHostElementRemovedDiagnostic((string)service.Attribute("name"));
                host.Remove();
            }
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

            var originalName = ((string)service.Attribute("name"))?.Trim();
            var uniqueName = CreateUniqueServiceName(candidate, usedNames);
            service.SetAttributeValue("name", uniqueName);

            if (string.IsNullOrWhiteSpace(originalName))
            {
                state.AddServiceNameGeneratedDiagnostic(uniqueName);
            }
            else if (!string.Equals(originalName, uniqueName, StringComparison.Ordinal))
            {
                state.AddServiceNameChangedDiagnostic(originalName, uniqueName);
            }
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

    private static void EnsureUniqueEndpointNames(TransformationState state)
    {
        var usedNames = new HashSet<string>(StringComparer.Ordinal);
        var endpoints = state.ServiceModel.Element("services")?.Elements("service").Elements("endpoint").ToArray() ?? Array.Empty<XElement>();

        for (var index = 0; index < endpoints.Length; index++)
        {
            var endpoint = endpoints[index];
            var candidate = ((string)endpoint.Attribute("name"))?.Trim();
            if (string.IsNullOrWhiteSpace(candidate))
            {
                var binding = ((string)endpoint.Attribute("binding"))?.Trim();
                var contract = ((string)endpoint.Attribute("contract"))?.Trim();
                candidate = string.IsNullOrWhiteSpace(binding) || string.IsNullOrWhiteSpace(contract)
                    ? $"Endpoint{index + 1}"
                    : $"{binding}_{contract}";
            }

            var originalName = ((string)endpoint.Attribute("name"))?.Trim();
            var uniqueName = CreateUniqueEndpointName(candidate, usedNames);
            endpoint.SetAttributeValue("name", uniqueName);

            if (string.IsNullOrWhiteSpace(originalName))
            {
                var binding = ((string)endpoint.Attribute("binding"))?.Trim();
                var contract = ((string)endpoint.Attribute("contract"))?.Trim();
                state.AddEndpointNameGeneratedDiagnostic(uniqueName, binding, contract);
            }
            else if (!string.Equals(originalName, uniqueName, StringComparison.Ordinal))
            {
                state.AddEndpointNameChangedDiagnostic(originalName, uniqueName);
            }
        }
    }

    private static string CreateUniqueEndpointName(string candidate, ISet<string> usedNames)
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
            .ToList()
            .ForEach(element =>
            {
                state.AddUnsupportedSectionRemovedDiagnostic(element.Name.LocalName);
                element.Remove();
            });
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
                state.AddUnsupportedBindingRemovedDiagnostic(binding.Name.LocalName);
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

        var unsupportedElements = state.ServiceModel
            .Element("bindings")?
            .Elements()
            .Elements("binding")
            .Elements("reliableSession")
            .ToArray() ?? Array.Empty<XElement>();

        foreach (var unsupportedElement in unsupportedElements)
        {
            var binding = unsupportedElement.Parent;
            var bindingCollection = binding?.Parent;
            state.AddUnsupportedBindingElementRemovedDiagnostic(
                bindingCollection?.Name.LocalName,
                (string)binding?.Attribute("name"),
                unsupportedElement.Name.LocalName);
            unsupportedElement.Remove();
        }
    }

    private static void RemoveUnsupportedBindingAttributes(TransformationContext context, TransformationState state)
    {
        if (!context.Options.RemoveUnsupportedConfiguration)
        {
            return;
        }

        var unsupportedAttributes = state.ServiceModel
            .Element("bindings")?
            .Elements()
            .Elements("binding")
            .Elements("security")
            .Elements("transport")
            .Attributes("proxyCredentialType")
            .ToArray() ?? Array.Empty<XAttribute>();

        foreach (var unsupportedAttribute in unsupportedAttributes)
        {
            var transport = unsupportedAttribute.Parent;
            var binding = transport?.Parent?.Parent;
            var bindingCollection = binding?.Parent;
            state.AddUnsupportedBindingAttributeRemovedDiagnostic(
                bindingCollection?.Name.LocalName,
                (string)binding?.Attribute("name"),
                transport?.Name.LocalName,
                unsupportedAttribute.Name.LocalName);
            unsupportedAttribute.Remove();
        }
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
                state.AddUnsupportedEndpointRemovedDiagnostic(
                    (string)endpoint.Attribute("contract"),
                    binding);
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
                state.AddInvalidBaseAddressDiagnostic((string)service.Attribute("name"), value);
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

            if (!Uri.TryCreate(endpointAddress, UriKind.Absolute, out _))
            {
                state.AddEndpointAddressResolvedDiagnostic(
                    (string)service.Attribute("name"),
                    (string)endpoint.Attribute("name"),
                    endpointAddress,
                    endpointUri);
            }
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
            state.AddMissingBaseAddressDiagnostic(
                (string)service.Attribute("name"),
                scheme,
                endpointAddress);
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
