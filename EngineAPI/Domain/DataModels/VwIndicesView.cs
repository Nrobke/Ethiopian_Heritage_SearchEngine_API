using System;
using System.Collections.Generic;

namespace EngineAPI.Domain.DataModels
{
    public partial class VwIndicesView
    {
        public int Id { get; set; }
        public int Document { get; set; }
        public int Concept { get; set; }
        public double? ConceptWeight { get; set; }
        public string? Instance { get; set; }
        public string? Keyword { get; set; }
        public double? Tf { get; set; }
        public double? Idf { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public string Link { get; set; } = null!;
        public string ConceptDesc { get; set; } = null!;
    }
}
