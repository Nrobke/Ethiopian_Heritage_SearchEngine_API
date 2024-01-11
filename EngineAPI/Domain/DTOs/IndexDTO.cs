namespace EngineAPI.Domain.DTOs
{
    public class IndexDTO
    {
        public int Id { get; set; }
        public int Document { get; set; }
        public int Concept { get; set; }
        public int ConceptWeight { get; set; }
        public string? Instance { get; set; }
    }
}
