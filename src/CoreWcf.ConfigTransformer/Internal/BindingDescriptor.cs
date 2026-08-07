using System.Xml.Linq;

namespace CoreWcf.ConfigTransformer.Internal;

internal sealed class BindingDescriptor
{
    public BindingDescriptor(string name, EndpointTransport transport)
    {
        Name = Guard.NotNullOrWhiteSpace(name, nameof(name));
        Transport = transport;
    }

    public string Name { get; }

    public EndpointTransport Transport { get; }

    public EndpointTransport ResolveTransport(XElement bindingElement)
    {
        if (Transport != EndpointTransport.Http)
        {
            return Transport;
        }

        var mode = (string)bindingElement?.Element("security")?.Attribute("mode");
        return string.Equals(mode, "Transport", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(mode, "TransportWithMessageCredential", StringComparison.OrdinalIgnoreCase)
            ? EndpointTransport.Https
            : EndpointTransport.Http;
    }
}
