namespace WebApi.Helper;

public static class BuildQueryParams
{
    public static string Make(object? obj)
    {
        if (obj == null) return "";

        var props = obj.GetType().GetProperties();
        var pairs = props
            .Select(p => {
                var value = p.GetValue(obj);
                return value != null ? $"{p.Name}={value}" : null;
            })
            .Where(x => x != null);

        return string.Join("&", pairs!);
    }
}
