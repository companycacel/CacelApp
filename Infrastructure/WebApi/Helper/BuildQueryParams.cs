namespace WebApi.Helper;

public static class BuildQueryParams
{
    public static QueryBuildResult Make(object? obj)
    {
        if (obj == null)
            return new();

        var props = obj.GetType().GetProperties();

        string? url = null;
        string? format = null;
        string? id = null;

        var query = new List<string>();

        foreach (var p in props)
        {
            var value = p.GetValue(obj);
            if (value == null) continue;

            switch (p.Name)
            {
                case "url":
                    url = value.ToString();
                    break;

                case "format":
                    format = value.ToString();
                    break;

                case "id":
                    id = value.ToString();
                    break;

                default:
                    query.Add($"{Uri.EscapeDataString(p.Name)}={Uri.EscapeDataString(value.ToString()!)}");
                    break;
            }
        }

        return new QueryBuildResult
        {
            Url = url ?? "",
            Id = id,
            Accept = format ?? "application/octet-stream",
            Query = string.Join("&", query)
        };
    }
}
public sealed class QueryBuildResult
{
    public string Url { get; init; } = "";
    public string Accept { get; init; } = "application/octet-stream";
    public string? Id { get; init; }
    public string Query { get; init; } = "";
}