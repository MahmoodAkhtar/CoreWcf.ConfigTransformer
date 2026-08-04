using System.Xml.Linq;

namespace CoreWcf.ConfigTransformer.Internal;

internal sealed class BindingConfigurationIndex
{
    private readonly Dictionary<string, XElement> _bindings;

    private BindingConfigurationIndex(Dictionary<string, XElement> bindings)
    {
        _bindings = Guard.NotNull(bindings, nameof(bindings));
    }

    public static BindingConfigurationIndex Create(XElement serviceModel)
    {
        Guard.NotNull(serviceModel, nameof(serviceModel));
        
        var bindings = serviceModel.Element("bindings")?
            .Elements()
            .SelectMany(bindingType => bindingType.Elements("binding"))
            .Where(binding => !string.IsNullOrWhiteSpace((string)binding.Attribute("name")))
            .ToDictionary(
                binding => $"{binding.Parent?.Name.LocalName}:{(string)binding.Attribute("name")}",
                binding => binding,
                StringComparer.OrdinalIgnoreCase) ?? new Dictionary<string, XElement>(StringComparer.OrdinalIgnoreCase);

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
