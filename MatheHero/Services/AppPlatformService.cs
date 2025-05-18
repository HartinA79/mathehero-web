using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MatheHero.Shared.Shared.Interface;

namespace MatheHero.Services
{
    public class AppPlatformService : IPlatformService
    {
        public bool IsRunningInMaui() => true;
    }
}
