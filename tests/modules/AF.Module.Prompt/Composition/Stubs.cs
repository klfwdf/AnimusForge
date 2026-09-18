using System.Collections.Generic;
using System.Linq;

namespace AnimusForge
{
    // Minimal stand-ins for the production DTOs the routing stage touches; no game objects.
    public sealed class MentionedWorldEntities
    {
        public List<string> Entities = new List<string>();
        public bool IsEmpty => Entities.Count == 0;
        public MentionedWorldEntities Clone() => new MentionedWorldEntities { Entities = Entities.ToList() };
        public void Merge(MentionedWorldEntities other)
        {
            if (other == null) return;
            foreach (string e in other.Entities) if (!Entities.Contains(e)) Entities.Add(e);
        }
    }
}
