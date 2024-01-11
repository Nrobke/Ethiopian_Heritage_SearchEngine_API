using System;
using System.Collections.Generic;

namespace EngineAPI.Domain.DataModels
{
    public partial class Document
    {
        public Document()
        {
            Indices = new HashSet<Index>();
        }

        public int Id { get; set; }
        public string Link { get; set; } = null!;
        public string? Description { get; set; }
        public string Title { get; set; } = null!;

        public virtual ICollection<Index> Indices { get; set; }
    }
}
