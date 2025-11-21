using static SmartSupport.WebClient.Pages.Assistant;

namespace SmartSupport.WebClient.Models
{
    public sealed class AssistResponseDto
    {
        public string Answer { get; set; } = string.Empty;
        public AssistIntent Intent { get; set; } = AssistIntent.Unknown;
        public double Confidence { get; set; }
        public string? OrderId { get; set; }
        public AssistStatus? Status { get; set; }
        public DateTimeOffset? Eta { get; set; }
        public List<string> Actions { get; set; } = new();
        public List<CitationDto> Citations { get; set; } = new();
        public bool RawContextUsed { get; set; }
    }
}
