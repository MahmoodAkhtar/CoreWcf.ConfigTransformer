namespace CoreWcf.ConfigTransformer.Internal;

internal sealed class BindingDescriptorRegistry
{
    public static BindingDescriptorRegistry Default { get; } = new(new[]
    {
        new BindingDescriptor("basicHttpBinding", EndpointTransport.Http),
        new BindingDescriptor("netTcpBinding", EndpointTransport.NetTcp),
        new BindingDescriptor("webHttpBinding", EndpointTransport.Http),
        new BindingDescriptor("wsHttpBinding", EndpointTransport.Http),
    });

    private readonly Dictionary<string, BindingDescriptor> _descriptors;

    private BindingDescriptorRegistry(IEnumerable<BindingDescriptor> descriptors)
    {
        _descriptors = Guard.NotNull(descriptors, nameof(descriptors))
            .ToDictionary(descriptor => descriptor.Name, StringComparer.OrdinalIgnoreCase);
    }

    public bool IsSupported(string bindingName) => Find(bindingName) is not null;

    public BindingDescriptor Find(string bindingName)
    {
        if (string.IsNullOrWhiteSpace(bindingName))
        {
            return null;
        }

        _descriptors.TryGetValue(bindingName, out var descriptor);
        return descriptor;
    }
}
