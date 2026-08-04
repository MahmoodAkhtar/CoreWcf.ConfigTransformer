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
}
