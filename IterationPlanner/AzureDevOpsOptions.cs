namespace IterationPlanner;

/// <summary>Server-side Azure connection configuration. Never export or log this object.</summary>
public sealed class AzureDevOpsOptions
{
    public const string SectionName = "AzureDevOps";
    public string CollectionUrl { get; set; } = "";
    [System.Text.Json.Serialization.JsonIgnore]
    public string Pat { get; set; } = "";
    public string ApiVersion { get; set; } = "7.0";
    public int TimeoutSeconds { get; set; } = 30;
}

