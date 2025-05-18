using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MatheHero.Shared.Models;

namespace MatheHero.Shared.Shared.Service
{
    public class AvatarService
    {
        public List<AvatarModel> Avatare { get; private set; } = new();

        public bool IsLoaded => Avatare.Count > 0;

        public void SetAvatars(List<AvatarModel> avatars)
        {
            Avatare = avatars;
        }

        public void Clear()
        {
            Avatare.Clear();
        }
    }
}
