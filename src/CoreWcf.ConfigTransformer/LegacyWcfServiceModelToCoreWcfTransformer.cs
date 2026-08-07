using System.Xml.Linq;
using CoreWcf.ConfigTransformer.Internal;

namespace CoreWcf.ConfigTransformer;

/// <summary>
/// Transforms legacy WCF service model configuration into a CoreWCF-friendly service model.
/// </summary>
public sealed class LegacyWcfServiceModelToCoreWcfTransformer
{
    private static readonly IReadOnlyList<ITransformationRule> TransformationRules =
    [
        new RemoveUnsupportedConfigurationRule(BindingDescriptorRegistry.Default),
        new EnsureUniqueServiceNamesRule(),
        new RemoveUnsupportedEndpointsRule(BindingDescriptorRegistry.Default),
        new EnsureUniqueEndpointNamesRule(),
        new ResolveEndpointAddressesRule(BindingDescriptorRegistry.Default)
    ];

    /// <summary>
    /// Transforms the specified system.serviceModel configuration element.
    /// </summary>
    public LegacyWcfServiceModelTransformResult Transform(XElement serviceModel, LegacyWcfServiceModelTransformOptions options = null)
    {
        Guard.NotNull(serviceModel, nameof(serviceModel));

        var context = new TransformationContext(options ?? new LegacyWcfServiceModelTransformOptions());
        var state = new TransformationState(new XElement(serviceModel));

        TransformServiceModel(context, state);
        return CreateTransformResult(context, state);
    }

    /// <summary>
    /// Transforms the system.serviceModel element from a legacy configuration file and writes the transformed element to a generated configuration file.
    /// </summary>
    public LegacyWcfServiceModelTransformResult Transform(
        string legacyConfigurationPath,
        string generatedConfigurationPath,
        LegacyWcfServiceModelTransformOptions options = null)
    {
        Guard.NotNullOrWhiteSpace(legacyConfigurationPath, nameof(legacyConfigurationPath));
        Guard.NotNullOrWhiteSpace(generatedConfigurationPath, nameof(generatedConfigurationPath));

        if (!File.Exists(legacyConfigurationPath))
        {
            throw new FileNotFoundException("The legacy configuration file could not be found.", legacyConfigurationPath);
        }

        var generatedConfigurationDirectory = Path.GetDirectoryName(Path.GetFullPath(generatedConfigurationPath));
        if (!string.IsNullOrWhiteSpace(generatedConfigurationDirectory) && !Directory.Exists(generatedConfigurationDirectory))
        {
            throw new DirectoryNotFoundException($"The generated configuration directory could not be found: '{generatedConfigurationDirectory}'.");
        }

        var legacyConfiguration = XDocument.Load(legacyConfigurationPath, LoadOptions.PreserveWhitespace);
        var serviceModel = legacyConfiguration.Root?.Element("system.serviceModel");
        if (serviceModel is null)
        {
            throw new LegacyWcfServiceModelTransformException("The legacy configuration does not contain a system.serviceModel section.");
        }

        var transformOptions = options ?? new LegacyWcfServiceModelTransformOptions();
        var result = Transform(serviceModel, transformOptions);
        var generatedConfiguration = CreateGeneratedConfiguration(legacyConfiguration, serviceModel, result, transformOptions);

        generatedConfiguration.Save(generatedConfigurationPath);

        return result;
    }

    private static XDocument CreateGeneratedConfiguration(
        XDocument legacyConfiguration,
        XElement legacyServiceModel,
        LegacyWcfServiceModelTransformResult result,
        LegacyWcfServiceModelTransformOptions options)
    {
        if (options.GeneratedConfigurationMode == GeneratedConfigurationMode.ServiceModelOnly)
        {
            return new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement("configuration", result.ServiceModel));
        }

        var generatedConfiguration = new XDocument(legacyConfiguration);
        var generatedServiceModel = generatedConfiguration.Root?.Element(legacyServiceModel.Name);
        if (generatedServiceModel is null)
        {
            throw new LegacyWcfServiceModelTransformException("The generated configuration does not contain a system.serviceModel section.");
        }

        generatedServiceModel.ReplaceWith(result.ServiceModel);

        return generatedConfiguration;
    }

    private static void TransformServiceModel(TransformationContext context, TransformationState state)
    {
        foreach (var rule in TransformationRules)
        {
            rule.Apply(context, state);
        }
    }

    private static LegacyWcfServiceModelTransformResult CreateTransformResult(TransformationContext context, TransformationState state)
    {
        var diagnostics = state.Diagnostics;
        if (context.Options.ThrowOnError && diagnostics.Any(diagnostic => diagnostic.Severity == LegacyWcfServiceModelDiagnosticSeverity.Error))
        {
            throw new LegacyWcfServiceModelTransformException("The configuration could not be transformed because one or more errors were reported.");
        }

        return new LegacyWcfServiceModelTransformResult(
            state.ServiceModel,
            state.HttpListeners,
            state.HttpsListeners,
            state.NetTcpListeners,
            diagnostics);
    }
}
