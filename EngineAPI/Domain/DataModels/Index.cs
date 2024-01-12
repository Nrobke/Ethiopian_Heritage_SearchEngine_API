using System;
using System.Collections.Generic;

namespace EngineAPI.Domain.DataModels
{
    public partial class Index
    {
        public int Id { get; set; }
        public int Document { get; set; }
        public int Concept { get; set; }
        public double? ConceptWeight { get; set; }
        public string? Instance { get; set; }
        public string? Keyword { get; set; }
        public double? Tf { get; set; }
        public double? Idf { get; set; }

        public virtual Concept ConceptNavigation { get; set; } = null!;
        public virtual Document DocumentNavigation { get; set; } = null!;
    }
}
