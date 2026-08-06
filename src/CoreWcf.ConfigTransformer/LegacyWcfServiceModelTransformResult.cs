using System.Xml.Linq;
using CoreWcf.ConfigTransformer.Internal;

namespace CoreWcf.ConfigTransformer;

/// <summary>
/// Contains the transformed service model and derived listener addresses.
/// </summary>
public sealed class LegacyWcfServiceModelTransformResult
{
    internal LegacyWcfServiceModelTransformResult(
        XElement serviceModel, 
        IReadOnlyList<Uri> httpListeners, 
        IReadOnlyList<Uri> httpsListeners, 
        IReadOnlyList<Uri> netTcpListeners, 
        IReadOnlyList<LegacyWcfServiceModelDiagnostic> diagnostics)
    {
        Guard.NotNull(serviceModel, nameof(serviceModel));
        Guard.NotNull(httpListeners, nameof(httpListeners));
        Guard.NotNull(httpsListeners, nameof(httpsListeners));
        Guard.NotNull(netTcpListeners, nameof(netTcpListeners));
        Guard.NotNull(diagnostics, nameof(diagnostics));

        ServiceModel = serviceModel;
        HttpListeners = httpListeners;
        HttpsListeners = httpsListeners;
        NetTcpListeners = netTcpListeners;
        Diagnostics = diagnostics;
    }

    /// <summary>Gets the transformed system.serviceModel element.</summary>
    public XElement ServiceModel { get; }

    /// <summary>Gets derived HTTP listener base addresses.</summary>
    public IReadOnlyList<Uri> HttpListeners { get; }

    /// <summary>Gets derived HTTPS listener base addresses.</summary>
    public IReadOnlyList<Uri> HttpsListeners { get; }

    /// <summary>Gets derived Net.TCP listener base addresses.</summary>
    public IReadOnlyList<Uri> NetTcpListeners { get; }

    /// <summary>Gets diagnostics produced by the transformation.</summary>
    public IReadOnlyList<LegacyWcfServiceModelDiagnostic> Diagnostics { get; }

    /// <summary>Gets a value indicating whether any error diagnostics were produced.</summary>
    public bool HasErrors => Diagnostics.Any(diagnostic => diagnostic.Severity == LegacyWcfServiceModelDiagnosticSeverity.Error);
}
