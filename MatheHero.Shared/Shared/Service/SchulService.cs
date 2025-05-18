using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MatheHero.Shared.Models;

namespace MatheHero.Shared.Shared.Service
{
    public class SchulService
    {
        public List<SchuleModel> Schulen { get; private set; } = new();

        public bool IsLoaded => Schulen.Count > 0;

        public void SetSchulen(List<SchuleModel> schulListe)
        {
            Schulen = schulListe;
        }

        public void Clear()
        {
            Schulen.Clear();
        }
    }
}
