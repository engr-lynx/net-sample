using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AccountManagementService.Helper
{
    public class AppSetting
    {

        public string Secret { get; set; }
    }

    public class connectionString
    {
        public string instance_name { get; set; }
        public string catalog { get; set; }
        public string user_name { get; set; }
        public string user_hash { get; set; }
        public string _DB_Master { get; set; }
    }


    public class EmailSender
    {
        public string email_username { get; set; }
        public string email_password { get; set; }
        public string email_name { get; set; }
        public string host { get; set; }
        public int port { get; set; }
    }
    public class Default_Url
    {
        public string name { get; set; }
    }
}
