using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace AccountManagementService.Model
{
    public class CompanyAuthenticateRequest
    {

        public string company_code { get; set; }
    }

    public class CompanyAuthenticateResponse
    {

        public string series_code { get; set; }
        public string company_code { get; set; }
    }


    public class UserAuthenticateRequest
    {

        [Required]
        public string username { get; set; }

        [Required]
        public string password { get; set; }


        public string company_code { get; set; }

        public string series_code { get; set; }
    }

    public class AuthenticateRequest
    {

        [Required]
        public string username { get; set; }

        [Required]
        public string password { get; set; }


        public string company_code { get; set; }
    }

    public class AuthenticateResponse
    {
        public string id { get; set; }

        public string email_address { get; set; }

        public string type { get; set; }

        public string routing { get; set; }

        public bool lock_account { get; set; }

        public string Token { get; set; }

        public bool active { get; set; }

        public bool is_admin { get; set; }

        public bool approver { get; set; }

        public string series { get; set; }

        public string series_code { get; set; }

        public string access_level_id { get; set; }

        public string approval_level_id { get; set; }
        public string category_id { get; set; }

        public string company_id { get; set; }


    }
}
