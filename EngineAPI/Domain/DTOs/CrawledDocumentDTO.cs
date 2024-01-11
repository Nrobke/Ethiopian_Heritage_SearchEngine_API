namespace EngineAPI.Domain.DTOs;

public class CrawledDocumentDTO
{
    public string Site { get; set; }
    public string Title { get; set; }
    public string? Description { get; set; } = null;
    public List<string> Content { get; set; }
}
