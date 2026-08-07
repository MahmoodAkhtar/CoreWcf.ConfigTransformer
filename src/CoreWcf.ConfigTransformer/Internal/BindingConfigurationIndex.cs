using System.Xml.Linq;

namespace CoreWcf.ConfigTransformer.Internal;

internal sealed class BindingConfigurationIndex
{
    private readonly Dictionary<BindingConfigurationKey, XElement> _bindings;

    private BindingConfigurationIndex(Dictionary<BindingConfigurationKey, XElement> bindings)
    {
        _bindings = Guard.NotNull(bindings, nameof(bindings));
    }

    public static BindingConfigurationIndex Create(TransformationState state)
    {
        Guard.NotNull(state, nameof(state));

        var bindings = new Dictionary<BindingConfigurationKey, XElement>();
        var bindingElements = state.ServiceModel.Element("bindings")?
            .Elements()
            .SelectMany(bindingType => bindingType.Elements("binding"))
            .Where(binding => !string.IsNullOrWhiteSpace((string)binding.Attribute("name")))
            .ToArray() ?? Array.Empty<XElement>();

        foreach (var binding in bindingElements)
        {
            var bindingName = binding.Parent?.Name.LocalName;
            var bindingConfiguration = (string)binding.Attribute("name");
            var key = new BindingConfigurationKey(bindingName, bindingConfiguration);
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

        _bindings.TryGetValue(new BindingConfigurationKey(binding, bindingConfiguration), out var element);
        return element;
    }

    private sealed class BindingConfigurationKey : IEquatable<BindingConfigurationKey>
    {
        private readonly string _bindingName;
        private readonly string _bindingConfiguration;

        public BindingConfigurationKey(string bindingName, string bindingConfiguration)
        {
            _bindingName = bindingName ?? string.Empty;
            _bindingConfiguration = bindingConfiguration ?? string.Empty;
        }

        public bool Equals(BindingConfigurationKey other)
        {
            if (other is null)
            {
                return false;
            }

            return string.Equals(_bindingName, other._bindingName, StringComparison.Ordinal) &&
                   string.Equals(_bindingConfiguration, other._bindingConfiguration, StringComparison.Ordinal);
        }

        public override bool Equals(object obj) => Equals(obj as BindingConfigurationKey);

        public override int GetHashCode()
        {
            unchecked
            {
                return (StringComparer.Ordinal.GetHashCode(_bindingName) * 397) ^
                       StringComparer.Ordinal.GetHashCode(_bindingConfiguration);
            }
        }
    }
}
