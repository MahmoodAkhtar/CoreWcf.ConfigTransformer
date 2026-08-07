using System.Xml.Linq;

namespace CoreWcf.ConfigTransformer.Internal;

internal sealed class EnsureUniqueEndpointNamesRule : ITransformationRule
{
    public void Apply(TransformationContext context, TransformationState state)
    {
        Guard.NotNull(context, nameof(context));
        Guard.NotNull(state, nameof(state));

        var usedNames = new HashSet<string>(StringComparer.Ordinal);
        var endpoints = state.ServiceModel.Element("services")?.Elements("service").Elements("endpoint").ToArray() ?? Array.Empty<XElement>();

        for (var index = 0; index < endpoints.Length; index++)
        {
            var endpoint = endpoints[index];
            var candidate = ((string)endpoint.Attribute("name"))?.Trim();
            if (string.IsNullOrWhiteSpace(candidate))
            {
                var binding = ((string)endpoint.Attribute("binding"))?.Trim();
                var contract = ((string)endpoint.Attribute("contract"))?.Trim();
                candidate = string.IsNullOrWhiteSpace(binding) || string.IsNullOrWhiteSpace(contract)
                    ? $"Endpoint{index + 1}"
                    : $"{binding}_{contract}";
            }

            var originalName = ((string)endpoint.Attribute("name"))?.Trim();
            var uniqueName = CreateUniqueName(candidate, usedNames);
            endpoint.SetAttributeValue("name", uniqueName);

            if (string.IsNullOrWhiteSpace(originalName))
            {
                var binding = ((string)endpoint.Attribute("binding"))?.Trim();
                var contract = ((string)endpoint.Attribute("contract"))?.Trim();
                state.AddEndpointNameGeneratedDiagnostic(uniqueName, binding, contract);
            }
            else if (!string.Equals(originalName, uniqueName, StringComparison.Ordinal))
            {
                state.AddEndpointNameChangedDiagnostic(originalName, uniqueName);
            }
        }
    }

    private static string CreateUniqueName(string candidate, ISet<string> usedNames)
    {
        if (usedNames.Add(candidate))
        {
            return candidate;
        }

        for (var suffix = 2;; suffix++)
        {
            var uniqueName = $"{candidate}_{suffix}";
            if (usedNames.Add(uniqueName))
            {
                return uniqueName;
            }
        }
    }
}
