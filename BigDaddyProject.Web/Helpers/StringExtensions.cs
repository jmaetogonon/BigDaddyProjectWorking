namespace BigDaddyProject.Web.Helpers;

public static class StringExtensions
{
    public static string? NullIfEmpty(this string? s)
    {
        return string.IsNullOrWhiteSpace(s) ? null : s;
    }
}
