namespace PharmaGo.Infrastructure.Caching;

public class RedisSettings
{
    public const string SectionName = "Redis";

    public string? ConnectionString { get; init; }
    public string InstanceName { get; init; } = "PharmaGo:";
}
