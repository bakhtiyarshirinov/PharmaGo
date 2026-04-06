namespace PharmaGo.Infrastructure.Persistence;

public class DatabaseSeedSettings
{
    public const string SectionName = "DatabaseSeeding";

    public bool EnableDemoData { get; set; } = true;
    public bool AllowProductionSeeding { get; set; }
}
