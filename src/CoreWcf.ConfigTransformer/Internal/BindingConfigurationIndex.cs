using System.Xml.Linq;

namespace CoreWcf.ConfigTransformer.Internal;

internal sealed class BindingConfigurationIndex
{
    private readonly Dictionary<string, XElement> _bindings;

    private BindingConfigurationIndex(Dictionary<string, XElement> bindings)
    {
        _bindings = Guard.NotNull(bindings, nameof(bindings));
    }

    public static BindingConfigurationIndex Create(TransformationState state)
    {
        Guard.NotNull(state, nameof(state));

        var bindings = new Dictionary<string, XElement>(StringComparer.Ordinal);
        var bindingElements = state.ServiceModel.Element("bindings")?
            .Elements()
            .SelectMany(bindingType => bindingType.Elements("binding"))
            .Where(binding => !string.IsNullOrWhiteSpace((string)binding.Attribute("name")))
            .ToArray() ?? Array.Empty<XElement>();

        foreach (var binding in bindingElements)
        {
            var bindingName = binding.Parent?.Name.LocalName;
            var bindingConfiguration = (string)binding.Attribute("name");
            var key = $"{bindingName}:{bindingConfiguration}";
            if (bindings.ContainsKey(key))
            {
                state.AddDuplicateBindingConfigurationDiagnostic(bindingName, bindingConfiguration);
                continue;
            }

            bindings.Add(key, binding);
        }

        return new BindingConfigurationIndex(bindings);
    }

    public XElement Find(string binding, string bindingConfiguration)
    {
        if (string.IsNullOrWhiteSpace(binding) || string.IsNullOrWhiteSpace(bindingConfiguration))
        {
            return null;
        }

        _bindings.TryGetValue($"{binding}:{bindingConfiguration}", out var element);
        return element;
    }
}
