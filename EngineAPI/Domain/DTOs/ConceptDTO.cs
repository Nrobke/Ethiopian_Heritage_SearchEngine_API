namespace EngineAPI.Domain.DTOs;

public class ConceptDTO
{
    public int Id { get; set; }
    public string Concept1 { get; set; } = null!;
    public string? ParentConcept { get; set; }
    public string? ChildConcept { get; set; }
}
