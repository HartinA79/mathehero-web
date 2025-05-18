using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatheHero.Shared.Models
{
    public class RegisterModel
    {
        [Required]
        public string UserName { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        public string Rolle { get; set; }

        [Required] 
        public int KlassenStufe { get; set; }

        public string AvatarPfad { get; set; }

        public string SchulId { get; set; }

        public string UserId { get; set; }
    }
}
