namespace CoreWcf.ConfigTransformer;

/// <summary>
/// Represents a fatal configuration transformation failure.
/// </summary>
public sealed class LegacyWcfServiceModelTransformException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LegacyWcfServiceModelTransformException"/> class.
    /// </summary>
    public LegacyWcfServiceModelTransformException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LegacyWcfServiceModelTransformException"/> class.
    /// </summary>
    public LegacyWcfServiceModelTransformException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
