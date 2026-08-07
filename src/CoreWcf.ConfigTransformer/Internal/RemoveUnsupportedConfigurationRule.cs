using System.Xml.Linq;

namespace CoreWcf.ConfigTransformer.Internal;

internal sealed class RemoveUnsupportedConfigurationRule : ITransformationRule
{
    private static readonly ISet<string> SupportedServiceModelSections = new HashSet<string>(StringComparer.Ordinal)
    {
        "bindings",
        "services",
    };

    private readonly BindingDescriptorRegistry _bindings;

    public RemoveUnsupportedConfigurationRule(BindingDescriptorRegistry bindings)
    {
        _bindings = Guard.NotNull(bindings, nameof(bindings));
    }

    public void Apply(TransformationContext context, TransformationState state)
    {
        Guard.NotNull(context, nameof(context));
        Guard.NotNull(state, nameof(state));

        if (!context.Options.RemoveUnsupportedConfiguration)
        {
            return;
        }

        RemoveUnsupportedSections(state);
        RemoveUnsupportedBindings(state);
        RemoveUnsupportedBindingElements(state);
        RemoveUnsupportedBindingAttributes(state);
    }

    private void RemoveUnsupportedSections(TransformationState state)
    {
        state.ServiceModel
            .Elements()
            .Where(element => !SupportedServiceModelSections.Contains(element.Name.LocalName))
            .ToList()
            .ForEach(element =>
            {
                state.AddUnsupportedSectionRemovedDiagnostic(element.Name.LocalName);
                element.Remove();
            });
    }

    private void RemoveUnsupportedBindings(TransformationState state)
    {
        var bindings = state.ServiceModel.Element("bindings")?.Elements().ToArray() ?? Array.Empty<XElement>();
        foreach (var binding in bindings)
        {
            if (!_bindings.IsSupported(binding.Name.LocalName))
            {
                state.AddUnsupportedBindingRemovedDiagnostic(binding.Name.LocalName);
                binding.Remove();
            }
        }
    }

    private static void RemoveUnsupportedBindingElements(TransformationState state)
    {
        var unsupportedElements = state.ServiceModel
            .Element("bindings")?
            .Elements()
            .Elements("binding")
            .Elements("reliableSession")
            .ToArray() ?? Array.Empty<XElement>();

        foreach (var unsupportedElement in unsupportedElements)
        {
            var binding = unsupportedElement.Parent;
            var bindingCollection = binding?.Parent;
            state.AddUnsupportedBindingElementRemovedDiagnostic(
                bindingCollection?.Name.LocalName,
                (string)binding?.Attribute("name"),
                unsupportedElement.Name.LocalName);
            unsupportedElement.Remove();
        }
    }

    private static void RemoveUnsupportedBindingAttributes(TransformationState state)
    {
        var unsupportedAttributes = state.ServiceModel
            .Element("bindings")?
            .Elements()
            .Elements("binding")
            .Elements("security")
            .Elements("transport")
            .Attributes("proxyCredentialType")
            .ToArray() ?? Array.Empty<XAttribute>();

        foreach (var unsupportedAttribute in unsupportedAttributes)
        {
            var transport = unsupportedAttribute.Parent;
            var binding = transport?.Parent?.Parent;
            var bindingCollection = binding?.Parent;
            state.AddUnsupportedBindingAttributeRemovedDiagnostic(
                bindingCollection?.Name.LocalName,
                (string)binding?.Attribute("name"),
                transport?.Name.LocalName,
                unsupportedAttribute.Name.LocalName);
            unsupportedAttribute.Remove();
        }
    }
}
