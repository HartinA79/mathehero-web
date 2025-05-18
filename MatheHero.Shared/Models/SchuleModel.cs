using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatheHero.Shared.Models
{
    public class SchuleModel
    {
        public string Adresse { get; set; }

        public string DocumentId {  get; set; }

        public string Land { get; set; }

        public string Name { get; set; }    

        public string Ort { get; set; }

        public string Region { get; set; }

        public int SchulNummer { get; set; }

        public int ZipCode { get; set; }
    }
}
