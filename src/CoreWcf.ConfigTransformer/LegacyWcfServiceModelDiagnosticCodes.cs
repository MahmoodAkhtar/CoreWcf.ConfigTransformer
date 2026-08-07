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

    /// <summary>An unsupported system.serviceModel section was removed.</summary>
    public const string UnsupportedSectionRemoved = "CWCF005";

    /// <summary>An unsupported binding collection was removed.</summary>
    public const string UnsupportedBindingRemoved = "CWCF006";

    /// <summary>An unsupported binding element was removed.</summary>
    public const string UnsupportedBindingElementRemoved = "CWCF007";

    /// <summary>A service name was generated because no name was configured.</summary>
    public const string ServiceNameGenerated = "CWCF008";

    /// <summary>A service name was changed to make it unique.</summary>
    public const string ServiceNameChanged = "CWCF009";

    /// <summary>An endpoint name was generated because no name was configured.</summary>
    public const string EndpointNameGenerated = "CWCF010";

    /// <summary>An endpoint name was changed to make it unique.</summary>
    public const string EndpointNameChanged = "CWCF011";

    /// <summary>A relative endpoint address was resolved to an absolute address.</summary>
    public const string EndpointAddressResolved = "CWCF012";

    /// <summary>A service host element was removed after base addresses were processed.</summary>
    public const string HostElementRemoved = "CWCF013";

    /// <summary>An unsupported binding attribute was removed.</summary>
    public const string UnsupportedBindingAttributeRemoved = "CWCF014";

    /// <summary>A binding configuration name is duplicated within a binding collection.</summary>
    public const string DuplicateBindingConfiguration = "CWCF015";
}
