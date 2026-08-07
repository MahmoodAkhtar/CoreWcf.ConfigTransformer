namespace CoreWcf.ConfigTransformer;

/// <summary>
/// Controls how generated configuration files are written by the transformer.
/// </summary>
public enum GeneratedConfigurationMode
{
    /// <summary>
    /// Replaces the legacy system.serviceModel section in the original configuration document.
    /// </summary>
    ReplaceServiceModelInConfiguration,

    /// <summary>
    /// Writes a configuration document containing only the transformed system.serviceModel section.
    /// </summary>
    ServiceModelOnly
}
