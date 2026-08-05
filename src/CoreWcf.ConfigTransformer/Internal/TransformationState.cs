using System.Xml.Linq;

namespace CoreWcf.ConfigTransformer.Internal;

internal sealed class TransformationState
{
    public TransformationState(XElement serviceModel)
    {
        ServiceModel = Guard.NotNull(serviceModel, nameof(serviceModel));
    }

    private readonly HashSet<Uri> _httpListeners = new(ListenerUriComparer.Instance);
    private readonly HashSet<Uri> _httpsListeners = new(ListenerUriComparer.Instance);
    private readonly HashSet<Uri> _netTcpListeners = new(ListenerUriComparer.Instance);
    private readonly List<LegacyWcfServiceModelDiagnostic> _diagnostics = new();

    public XElement ServiceModel { get; }

    public IReadOnlyList<Uri> HttpListeners => _httpListeners.OrderBy(uri => uri.ToString(), StringComparer.OrdinalIgnoreCase).ToArray();

    public IReadOnlyList<Uri> HttpsListeners => _httpsListeners.OrderBy(uri => uri.ToString(), StringComparer.OrdinalIgnoreCase).ToArray();

    public IReadOnlyList<Uri> NetTcpListeners => _netTcpListeners.OrderBy(uri => uri.ToString(), StringComparer.OrdinalIgnoreCase).ToArray();

    public IReadOnlyList<LegacyWcfServiceModelDiagnostic> Diagnostics => _diagnostics.ToArray();

    public void AddListener(EndpointTransport transport, Uri uri)
    {
        Guard.NotNull(uri, nameof(uri));
        
        switch (transport)
        {
            case EndpointTransport.Http:
                _httpListeners.Add(uri);
                break;
            case EndpointTransport.Https:
                _httpsListeners.Add(uri);
                break;
            case EndpointTransport.NetTcp:
                _netTcpListeners.Add(uri);
                break;
        }
    }

    public void AddDiagnostic(LegacyWcfServiceModelDiagnosticSeverity severity, string code, string message)
    {
        Guard.NotNullOrWhiteSpace(code, nameof(code));
        Guard.NotNullOrWhiteSpace(message, nameof(message));

        _diagnostics.Add(new LegacyWcfServiceModelDiagnostic(severity, code, message));
    }

    public void AddUnsupportedSectionRemovedDiagnostic(string sectionName)
    {
        AddDiagnostic(
            LegacyWcfServiceModelDiagnosticSeverity.Warning,
            LegacyWcfServiceModelDiagnosticCodes.UnsupportedSectionRemoved,
            $"Removed unsupported system.serviceModel section '{sectionName}'.");
    }

    public void AddUnsupportedBindingRemovedDiagnostic(string bindingName)
    {
        AddDiagnostic(
            LegacyWcfServiceModelDiagnosticSeverity.Warning,
            LegacyWcfServiceModelDiagnosticCodes.UnsupportedBindingRemoved,
            $"Removed unsupported binding collection '{bindingName}'.");
    }

    public void AddUnsupportedBindingElementRemovedDiagnostic(string bindingName, string bindingConfiguration, string elementName)
    {
        AddDiagnostic(
            LegacyWcfServiceModelDiagnosticSeverity.Warning,
            LegacyWcfServiceModelDiagnosticCodes.UnsupportedBindingElementRemoved,
            $"Removed unsupported element '{elementName}' from binding '{bindingName}' with configuration '{bindingConfiguration}'.");
    }

    public void AddUnsupportedEndpointRemovedDiagnostic(string contract, string binding)
    {
        AddDiagnostic(
            LegacyWcfServiceModelDiagnosticSeverity.Warning,
            LegacyWcfServiceModelDiagnosticCodes.UnsupportedBinding,
            $"Removed endpoint '{contract}' because it uses unsupported binding '{binding}'.");
    }

    public void AddInvalidBaseAddressDiagnostic(string serviceName, string baseAddress)
    {
        AddDiagnostic(
            LegacyWcfServiceModelDiagnosticSeverity.Error,
            LegacyWcfServiceModelDiagnosticCodes.InvalidBaseAddress,
            $"Service '{serviceName}' has invalid base address '{baseAddress}'.");
    }

    public void AddMissingBaseAddressDiagnostic(string serviceName, string scheme, string endpointAddress)
    {
        AddDiagnostic(
            LegacyWcfServiceModelDiagnosticSeverity.Error,
            LegacyWcfServiceModelDiagnosticCodes.MissingBaseAddress,
            $"Service '{serviceName}' has no '{scheme}' base address for endpoint '{endpointAddress}'.");
    }

    public void AddServiceNameGeneratedDiagnostic(string generatedName)
    {
        AddDiagnostic(
            LegacyWcfServiceModelDiagnosticSeverity.Information,
            LegacyWcfServiceModelDiagnosticCodes.ServiceNameGenerated,
            $"Generated service name '{generatedName}' because no service name was configured.");
    }

    public void AddServiceNameChangedDiagnostic(string originalName, string newName)
    {
        AddDiagnostic(
            LegacyWcfServiceModelDiagnosticSeverity.Warning,
            LegacyWcfServiceModelDiagnosticCodes.ServiceNameChanged,
            $"Changed service name from '{originalName}' to '{newName}' to make it unique.");
    }

    public void AddEndpointNameGeneratedDiagnostic(string generatedName, string binding, string contract)
    {
        AddDiagnostic(
            LegacyWcfServiceModelDiagnosticSeverity.Information,
            LegacyWcfServiceModelDiagnosticCodes.EndpointNameGenerated,
            $"Generated endpoint name '{generatedName}' from binding '{binding}' and contract '{contract}'.");
    }

    public void AddEndpointNameChangedDiagnostic(string originalName, string newName)
    {
        AddDiagnostic(
            LegacyWcfServiceModelDiagnosticSeverity.Warning,
            LegacyWcfServiceModelDiagnosticCodes.EndpointNameChanged,
            $"Changed endpoint name from '{originalName}' to '{newName}' to make it unique.");
    }

    public void AddEndpointAddressResolvedDiagnostic(string serviceName, string endpointName, string originalAddress, Uri resolvedAddress)
    {
        AddDiagnostic(
            LegacyWcfServiceModelDiagnosticSeverity.Information,
            LegacyWcfServiceModelDiagnosticCodes.EndpointAddressResolved,
            $"Resolved endpoint '{endpointName}' for service '{serviceName}' from address '{originalAddress}' to '{resolvedAddress}'.");
    }

    public void AddHostElementRemovedDiagnostic(string serviceName)
    {
        AddDiagnostic(
            LegacyWcfServiceModelDiagnosticSeverity.Information,
            LegacyWcfServiceModelDiagnosticCodes.HostElementRemoved,
            $"Removed host element from service '{serviceName}' after processing base addresses.");
    }
}
