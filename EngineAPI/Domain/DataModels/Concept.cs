using System;
using System.Collections.Generic;

namespace EngineAPI.Domain.DataModels
{
    public partial class Concept
    {
        public Concept()
        {
            Indices = new HashSet<Index>();
        }

        public int Id { get; set; }
        public string Concept1 { get; set; } = null!;
        public string? ParentConcept { get; set; }
        public string? ChildConcept { get; set; }

        public virtual ICollection<Index> Indices { get; set; }
    }
}
