namespace Watchmen.Common.Configuration;

public sealed class ApiKeyConfiguration
{
    public List<ApiKey> Keys { get; set; } = [];
}

public sealed record ApiKey
{
    public required string Id { get; init; }
    public required string Key { get; init; }
    public required string Name { get; init; }
    public required string[] Scopes { get; init; }
    public bool IsActive { get; init; } = true;
    public DateTime CreatedAt { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public string? CreatedBy { get; init; }
}
