using System;
using System.Collections.Generic;

namespace EngineAPI.Domain.DataModels
{
    public partial class VwIndicesView
    {
        public int Document { get; set; }
        public int Concept { get; set; }
        public string? Instance { get; set; }
        public double? Tf { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public string Link { get; set; } = null!;
        public string ConceptDesc { get; set; } = null!;
        public string? ParentConcept { get; set; }
    }
}
