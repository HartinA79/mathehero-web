using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatheHero.Shared.Helper
{
    public class FirestoreQueryResult<T>
    {
        public FirestoreDocument<T> Document { get; set; }
    }

    public class FirestoreDocument<T>
    {
        public string Name { get; set; }

        public T Fields { get; set; }
    }
}
