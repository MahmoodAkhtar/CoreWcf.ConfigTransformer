using System.Xml.Linq;

namespace CoreWcf.ConfigTransformer.Internal;

internal sealed class RemoveUnsupportedEndpointsRule : ITransformationRule
{
    private readonly BindingDescriptorRegistry _bindings;

    public RemoveUnsupportedEndpointsRule(BindingDescriptorRegistry bindings)
    {
        _bindings = Guard.NotNull(bindings, nameof(bindings));
    }

    public void Apply(TransformationContext context, TransformationState state)
    {
        Guard.NotNull(context, nameof(context));
        Guard.NotNull(state, nameof(state));

        var endpoints = state.ServiceModel.Element("services")?.Elements("service").Elements("endpoint").ToArray() ?? Array.Empty<XElement>();
        foreach (var endpoint in endpoints)
        {
            var binding = ((string)endpoint.Attribute("binding"))?.Trim();
            if (_bindings.IsSupported(binding))
            {
                continue;
            }

            if (context.Options.RemoveUnsupportedConfiguration)
            {
                state.AddUnsupportedEndpointRemovedDiagnostic(
                    (string)endpoint.Attribute("contract"),
                    binding);
                endpoint.Remove();
            }
            else
            {
                state.AddUnsupportedEndpointDiagnostic(
                    (string)endpoint.Attribute("contract"),
                    binding);
            }
        }
    }
}
