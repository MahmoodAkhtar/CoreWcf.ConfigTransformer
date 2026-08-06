using CoreWcf.ConfigTransformer.Internal;

namespace CoreWcf.ConfigTransformer;

/// <summary>
/// Represents a diagnostic produced while transforming a WCF configuration file.
/// </summary>
public sealed class LegacyWcfServiceModelDiagnostic
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LegacyWcfServiceModelDiagnostic"/> class.
    /// </summary>
    public LegacyWcfServiceModelDiagnostic(
        LegacyWcfServiceModelDiagnosticSeverity severity, 
        string code, 
        string message)
    {
        Guard.NotNull(code, nameof(code));
        Guard.NotNull(message, nameof(message));

        Code = code;
        Severity = severity;
        Message = message;
    }

    /// <summary>Gets the diagnostic code.</summary>
    public string Code { get; }

    /// <summary>Gets the diagnostic severity.</summary>
    public LegacyWcfServiceModelDiagnosticSeverity Severity { get; }

    /// <summary>Gets the diagnostic message.</summary>
    public string Message { get; }

    /// <inheritdoc />
    public override string ToString() => $"{Code} {Severity}: {Message}";
}
