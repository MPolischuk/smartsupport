namespace SmartSupport.WebClient.Models
{
    public sealed class CitationDto
    {
        public string Source { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string? Table { get; set; }
        public string? Service { get; set; }
        public string? Id { get; set; }
        public int? Page { get; set; }
    }
}
