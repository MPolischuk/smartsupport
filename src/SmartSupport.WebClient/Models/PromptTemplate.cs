namespace SmartSupport.WebClient.Models
{
    public class PromptTemplate
    {
        public string Value { get; init; } = string.Empty;
        public string Label { get; init; } = string.Empty;
        public string? OrderNumber { get; init; }
        public bool UseSql { get; init; }
        public bool UseApi { get; init; }
    }
}
