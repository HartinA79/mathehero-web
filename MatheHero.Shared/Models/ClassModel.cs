using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatheHero.Shared.Models
{
    public class ClassModel
    {
        public string DokumentId { get; set; }

        public string Name { get; set; }

        public string TeacherId { get; set; }

        public DateTime CreatedAt { get; set; }

        public List<string> UnlockedCategories { get; set; } = new();

        public List<string> UnlockedSubcategories { get; set; } = new();
    }
}
