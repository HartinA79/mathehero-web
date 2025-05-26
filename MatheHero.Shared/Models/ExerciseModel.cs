using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MatheHero.Shared.Shared.Pages.Student.Components.Exercises;

namespace MatheHero.Shared.Models
{
    public class ExerciseModel
    {
        public string DokumentId { get; set; }

        public string Name { get; set; }

        public int Index { get; set; }

        public string Icon { get; set; }

        public bool IsUnlocked { get; set; } = false;

        public int ProgressPercent { get; set; } = 0;

        public List<SubcategoryModel> Subcategories { get; set; } = new();

        public DateTime CreatedAt { get; set; }
    }

    public class SubcategoryModel
    {
        public string DokumentId { get; set; }

        public string Name { get; set; }

        public int Index { get; set; }

        public bool IsUnlocked { get; set; } = false;

        public int ProgressPercent { get; set; } = 0;
    }
}
