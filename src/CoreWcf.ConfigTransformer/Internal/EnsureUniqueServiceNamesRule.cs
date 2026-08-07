using System.Xml.Linq;

namespace CoreWcf.ConfigTransformer.Internal;

internal sealed class EnsureUniqueServiceNamesRule : ITransformationRule
{
    public void Apply(TransformationContext context, TransformationState state)
    {
        Guard.NotNull(context, nameof(context));
        Guard.NotNull(state, nameof(state));

        var usedNames = new HashSet<string>(StringComparer.Ordinal);
        var services = state.ServiceModel.Element("services")?.Elements("service").ToArray() ?? Array.Empty<XElement>();

        for (var index = 0; index < services.Length; index++)
        {
            var service = services[index];
            var candidate = ((string)service.Attribute("name"))?.Trim();
            if (string.IsNullOrWhiteSpace(candidate))
            {
                candidate = ((string)service.Elements("endpoint").FirstOrDefault()?.Attribute("contract"))?.Trim();
            }

            if (string.IsNullOrWhiteSpace(candidate))
            {
                candidate = $"Service{index + 1}";
            }

            var originalName = ((string)service.Attribute("name"))?.Trim();
            var uniqueName = CreateUniqueName(candidate, usedNames);
            service.SetAttributeValue("name", uniqueName);

            if (string.IsNullOrWhiteSpace(originalName))
            {
                state.AddServiceNameGeneratedDiagnostic(uniqueName);
            }
            else if (!string.Equals(originalName, uniqueName, StringComparison.Ordinal))
            {
                state.AddServiceNameChangedDiagnostic(originalName, uniqueName);
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
