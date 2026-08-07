namespace CoreWcf.ConfigTransformer.Internal;

internal interface ITransformationRule
{
    void Apply(TransformationContext context, TransformationState state);
}
