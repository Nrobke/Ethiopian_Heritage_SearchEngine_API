namespace EngineAPI.Domain.DTOs
{
    public class IndexDTO
    {
        public int Id { get; set; }
        public int Document { get; set; }
        public int Concept { get; set; }
        public double ConceptWeight { get; set; }
        public string? Instance { get; set; }
        public string? Keyword { get; set; }
        public double Tf { get; set; }
        public double Idf { get; set; }
    }
}
