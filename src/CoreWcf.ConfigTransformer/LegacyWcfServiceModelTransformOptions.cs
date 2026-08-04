namespace CoreWcf.ConfigTransformer;

/// <summary>
/// Controls how WCF configuration files are transformed for CoreWCF.
/// </summary>
public sealed class LegacyWcfServiceModelTransformOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether unsupported WCF-only configuration should be removed.
    /// </summary>
    public bool RemoveUnsupportedConfiguration { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether diagnostics with error severity throw an exception.
    /// </summary>
    public bool ThrowOnError { get; set; }
}
