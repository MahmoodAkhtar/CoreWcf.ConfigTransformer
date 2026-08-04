namespace CoreWcf.ConfigTransformer;

/// <summary>
/// Diagnostic codes emitted by the CoreWCF configuration transformer.
/// </summary>
public static class LegacyWcfServiceModelDiagnosticCodes
{
    /// <summary>The configuration file does not contain a system.serviceModel section.</summary>
    public const string MissingServiceModel = "CWCF001";

    /// <summary>A service has no host base address for one or more endpoints.</summary>
    public const string MissingBaseAddress = "CWCF002";

    /// <summary>A configured base address is not an absolute URI.</summary>
    public const string InvalidBaseAddress = "CWCF003";

    /// <summary>An endpoint uses a binding that cannot be mapped to a known transport.</summary>
    public const string UnsupportedBinding = "CWCF004";
}
