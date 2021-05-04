using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace AccountManagementService.Model
{
    public class RegistrationRequest
    {
        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public string email_address { get; set; }

        [Required]
        public bool active { get; set; }
    }

    public class RegistrationResponse
    {

        public string email_address { get; set; }
        public string id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool active { get; set; }

    }
}
