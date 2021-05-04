using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace AccountManagementService.Model
{
    public class VerificationRequest
    {

        [Required]
        public string id { get; set; }
    }

    public class VerificationResponse
    {

        public string guid { get; set; }
        public string id { get; set; }
    }
}
