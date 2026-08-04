namespace CoreWcf.ConfigTransformer.Internal;

internal sealed class ListenerUriComparer : IEqualityComparer<Uri>
{
    public static ListenerUriComparer Instance { get; } = new();

    public bool Equals(Uri x, Uri y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if (x is null || y is null)
        {
            return false;
        }

        return string.Equals(Normalize(x), Normalize(y), StringComparison.OrdinalIgnoreCase);
    }

    public int GetHashCode(Uri obj) => StringComparer.OrdinalIgnoreCase.GetHashCode(Normalize(obj));

    private static string Normalize(Uri uri) => uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
}
