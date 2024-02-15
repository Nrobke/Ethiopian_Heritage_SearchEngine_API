namespace EngineAPI.Domain.Misc;

public class SimilarityDTO
{
    public string ConceptOne { get; set; } = string.Empty;
    public string ConceptTwo { get; set;} = string.Empty;
    public double Similarity { get; set;}
}
