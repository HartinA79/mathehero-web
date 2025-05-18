using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatheHero.Shared.Helper
{
    public class FirestoreResult
    {
        public bool Success { get; set; }

        public string Message { get; set; }

        public string DocumentId { get; set; }
    }
}
