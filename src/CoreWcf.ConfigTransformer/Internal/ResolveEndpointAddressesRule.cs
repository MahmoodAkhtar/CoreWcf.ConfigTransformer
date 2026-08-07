using System.Xml.Linq;

namespace CoreWcf.ConfigTransformer.Internal;

internal sealed class ResolveEndpointAddressesRule : ITransformationRule
{
    private readonly BindingDescriptorRegistry _bindings;

    public ResolveEndpointAddressesRule(BindingDescriptorRegistry bindings)
    {
        _bindings = Guard.NotNull(bindings, nameof(bindings));
    }

    public void Apply(TransformationContext context, TransformationState state)
    {
        Guard.NotNull(context, nameof(context));
        Guard.NotNull(state, nameof(state));

        var bindingIndex = BindingConfigurationIndex.Create(state);
        foreach (var service in state.ServiceModel.Element("services")?.Elements("service") ?? Enumerable.Empty<XElement>())
        {
            var baseAddresses = ReadBaseAddresses(state, service);
            foreach (var endpoint in service.Elements("endpoint"))
            {
                ProcessEndpoint(state, bindingIndex, service, endpoint, baseAddresses);
            }

            var host = service.Element("host");
            if (host is not null && context.Options.RemoveUnsupportedConfiguration)
            {
                state.AddHostElementRemovedDiagnostic((string)service.Attribute("name"));
                host.Remove();
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

    private void ProcessEndpoint(TransformationState state, BindingConfigurationIndex bindingIndex, XElement service, XElement endpoint, IReadOnlyList<Uri> baseAddresses)
    {
        var binding = ((string)endpoint.Attribute("binding"))?.Trim();
        var bindingConfiguration = ((string)endpoint.Attribute("bindingConfiguration"))?.Trim();
        var endpointAddress = ((string)endpoint.Attribute("address"))?.Trim();
        var bindingElement = bindingIndex.Find(binding, bindingConfiguration);
        var descriptor = _bindings.Find(binding);
        var transport = descriptor?.ResolveTransport(bindingElement) ?? EndpointTransport.Unknown;

        if (transport == EndpointTransport.Unknown)
        {
            return;
        }

        var endpointUri = ResolveEndpointAddress(state, service, transport, endpointAddress, baseAddresses);
        if (endpointUri is null)
        {
            return;
        }

        endpoint.SetAttributeValue("address", endpointUri.ToString());
        AddListener(state, endpointUri);

        if (!Uri.TryCreate(endpointAddress, UriKind.Absolute, out _))
        {
            state.AddEndpointAddressResolvedDiagnostic(
                (string)service.Attribute("name"),
                (string)endpoint.Attribute("name"),
                endpointAddress,
                endpointUri);
        }
    }

    private static void AddListener(TransformationState state, Uri endpointUri)
    {
        switch (endpointUri.Scheme.ToLowerInvariant())
        {
            case "http":
                state.AddListener(EndpointTransport.Http, endpointUri);
                break;
            case "https":
                state.AddListener(EndpointTransport.Https, endpointUri);
                break;
            case "net.tcp":
                state.AddListener(EndpointTransport.NetTcp, endpointUri);
                break;
        }
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

        return new Uri(EnsureTrailingSlash(baseAddress), endpointAddress.TrimStart('/'));
    }

    private static Uri EnsureTrailingSlash(Uri uri)
    {
        var value = uri.ToString();
        return value.EndsWith("/", StringComparison.Ordinal) ? uri : new Uri($"{value}/");
    }
}
