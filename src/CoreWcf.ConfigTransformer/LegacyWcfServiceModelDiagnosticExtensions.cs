using CoreWcf.ConfigTransformer.Internal;

namespace CoreWcf.ConfigTransformer;

/// <summary>
/// Provides extension methods for writing WCF transformation diagnostics.
/// </summary>
public static class LegacyWcfServiceModelDiagnosticExtensions
{
    /// <summary>
    /// Writes each diagnostic to the specified text writer.
    /// </summary>
    public static void WriteTo(
        this IEnumerable<LegacyWcfServiceModelDiagnostic> diagnostics, 
        TextWriter writer)
    {
        Guard.NotNull(diagnostics, nameof(diagnostics));
        Guard.NotNull(writer, nameof(writer));
        
        foreach (var diagnostic in diagnostics)
        {
            writer.WriteLine(diagnostic.ToString());
        }
    }
}
