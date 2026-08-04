namespace CoreWcf.ConfigTransformer.Internal;

internal sealed class TransformationContext
{
    public TransformationContext(LegacyWcfServiceModelTransformOptions options)
    {
        Options = Guard.NotNull(options, nameof(options));
    }

    public LegacyWcfServiceModelTransformOptions Options { get; }
}
