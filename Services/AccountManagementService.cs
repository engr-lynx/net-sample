
using System;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data;
using AccountManagementService.Model;
using AccountManagementService.Helper;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using System.IO;
using Microsoft.AspNetCore.Hosting;

using Amazon.Lambda.Core;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace AccountManagementService.Services
{
    public interface IAccountManagementService
    {
        RegistrationResponse Registration(RegistrationRequest model);
        VerificationResponse Verification(VerificationRequest model);
        CompanyAuthenticateResponse CompanyAuthentication(string company_code);
        AuthenticateResponse AuthenticateLogin(AuthenticateRequest model);
        AuthenticateResponse GetById(int userId);
        CompanyResponse CompanyIU(CompanyIURequest model);
        List<CompanyResponse> company_view_sel(string company_id, string created_by);
        CompanyInActive company_in_active(string company_id);

    }
    public class AccountManagementServices : IAccountManagementService
    {
        public RegistrationResponse resp = new RegistrationResponse();

        private connectionString connection { get; set; }
        private readonly AppSetting _appSettings;

        private readonly IWebHostEnvironment _environment;

        public string FunctionHandler(string input, ILambdaContext context)
        {
            Console.WriteLine("Test");
            Console.WriteLine(input);
            return input?.ToUpper();
        }

        public AccountManagementServices(IOptions<AppSetting> appSettings, IWebHostEnvironment IWebHostEnvironment, IOptions<connectionString> settings)
        {
            _appSettings = appSettings.Value;
            connection = settings.Value;

            _environment = IWebHostEnvironment;
        }

        public RegistrationResponse Registration(RegistrationRequest model)
        {
            // validation
            //if (string.IsNullOrWhiteSpace(model.Password))
            //    throw new AppException("Password is required");

            RegistrationResponse resp = new RegistrationResponse();
            string _con = connection._DB_Master;
            DataTable dt = new DataTable();
            string UserHash = Crypto.password_encrypt(model.Password);
            SqlConnection oConn = new SqlConnection(_con);
            SqlTransaction oTrans;
            oConn.Open();
            oTrans = oConn.BeginTransaction();
            SqlCommand oCmd = new SqlCommand();
            oCmd.Connection = oConn;
            oCmd.Transaction = oTrans;
            try
            {
                oCmd.CommandText = "users_in";
                oCmd.CommandType = CommandType.StoredProcedure;
                oCmd.Parameters.Clear();
                oCmd.Parameters.AddWithValue("@user_name", model.Username);
                oCmd.Parameters.AddWithValue("@user_hash", UserHash);
                oCmd.Parameters.AddWithValue("@email_address", model.email_address);
                //oCmd.Parameters.AddWithValue("@active", model.active);
                SqlDataReader sdr = oCmd.ExecuteReader();
                while (sdr.Read())
                {
                    resp.id = Crypto.url_encrypt(sdr["user_id"].ToString());
                    resp.email_address = sdr["email_address"].ToString();
                }
                sdr.Close();
                oTrans.Commit();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }
            finally
            {
                oConn.Close();
            }

            return resp;
        }

        public VerificationResponse Verification(VerificationRequest model)
        {


            VerificationResponse resp = new VerificationResponse();
            string _con = connection._DB_Master;
            DataTable dt = new DataTable();
            SqlConnection oConn = new SqlConnection(_con);
            SqlTransaction oTrans;
            oConn.Open();
            oTrans = oConn.BeginTransaction();
            SqlCommand oCmd = new SqlCommand();
            oCmd.Connection = oConn;
            oCmd.Transaction = oTrans;
            try
            {
                oCmd.CommandText = "users_verification";
                oCmd.CommandType = CommandType.StoredProcedure;
                oCmd.Parameters.Clear();
                oCmd.Parameters.AddWithValue("@user_id", Crypto.url_decrypt(model.id));

                SqlDataReader sdr = oCmd.ExecuteReader();
                while (sdr.Read())
                {
                    resp.id = Crypto.url_encrypt(sdr["user_id"].ToString());
                }
                sdr.Close();
                oTrans.Commit();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }
            finally
            {
                oConn.Close();
            }

            return resp;
        }

        public AuthenticateResponse AuthenticateLogin(AuthenticateRequest model)
        {
            AuthenticateResponse resp = new AuthenticateResponse();


            string _con = connection._DB_Master;
            DataTable dt = new DataTable();
            string UserHash = Crypto.password_encrypt(model.password);
            SqlConnection oConn = new SqlConnection(_con);
            SqlTransaction oTrans;

            oConn.Open();
            oTrans = oConn.BeginTransaction();
            SqlCommand oCmd = new SqlCommand();
            oCmd.Connection = oConn;
            oCmd.Transaction = oTrans;
            try
            {

                oCmd.CommandText = "login_authentication";
                oCmd.CommandType = CommandType.StoredProcedure;
                oCmd.Parameters.Clear();
                oCmd.Parameters.AddWithValue("@user_name", model.username);
                oCmd.Parameters.AddWithValue("@user_hash", UserHash);
                oCmd.Parameters.AddWithValue("@company_code", model.company_code);
                SqlDataReader sdr = oCmd.ExecuteReader();
                while (sdr.Read())
                {

                    resp.id = sdr["user_id"].ToString() == "0" ? "0" : Crypto.url_encrypt(sdr["user_id"].ToString());
                    resp.routing = sdr["routing"].ToString();
                    resp.type = sdr["type"].ToString();
                    resp.active = Convert.ToBoolean(sdr["active"].ToString());
                    resp.lock_account = Convert.ToBoolean(sdr["lock_account"].ToString());
                    resp.series = sdr["series_code"].ToString();
                    resp.series_code = Crypto.url_encrypt(sdr["series_code"].ToString());
                    resp.access_level_id = Crypto.url_encrypt(sdr["access_level_id"].ToString());
                    resp.approval_level_id = Crypto.url_encrypt(sdr["approval_level_id"].ToString());
                    resp.category_id = Crypto.url_encrypt(sdr["category_id"].ToString());
                    resp.company_id = Crypto.url_encrypt(sdr["company_id"].ToString());
                    resp.is_admin = Convert.ToBoolean(sdr["is_admin"].ToString());
                    resp.approver = Convert.ToBoolean(sdr["approver"].ToString());
                }
                sdr.Close();
                oConn.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }
            finally
            {
                oConn.Close();
            }



            if (resp.id != "" && resp.id != "0")
            {

                resp.Token = generateJwtToken(resp);
            }



            return resp;
        }


        public CompanyAuthenticateResponse CompanyAuthentication(string company_code)
        {
            CompanyAuthenticateResponse resp = new CompanyAuthenticateResponse();


            string _con = connection._DB_Master;
            DataTable dt = new DataTable();
            SqlConnection oConn = new SqlConnection(_con);
            SqlTransaction oTrans;

            oConn.Open();
            oTrans = oConn.BeginTransaction();
            SqlCommand oCmd = new SqlCommand();
            oCmd.Connection = oConn;
            oCmd.Transaction = oTrans;
            try
            {
                oCmd.CommandText = "company_authentication";
                oCmd.CommandType = CommandType.StoredProcedure;
                oCmd.Parameters.Clear();
                oCmd.Parameters.AddWithValue("@company_code", company_code);
                SqlDataReader sdr = oCmd.ExecuteReader();
                while (sdr.Read())
                {

                    resp.company_code = sdr["company_code"].ToString();
                    resp.series_code = sdr["series_code"].ToString();
                }
                sdr.Close();
                oConn.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }
            finally
            {
                oConn.Close();
            }



            return resp;
        }

        public CompanyResponse CompanyIU(CompanyIURequest model)
        {

            CompanyResponse resp = new CompanyResponse();
            string _con = connection._DB_Master;
            DataTable dt = new DataTable();
            SqlConnection oConn = new SqlConnection(_con);
            SqlTransaction oTrans;
            oConn.Open();
            oTrans = oConn.BeginTransaction();
            SqlCommand oCmd = new SqlCommand();
            oCmd.Connection = oConn;
            oCmd.Transaction = oTrans;
            try
            {
                var folder = "code\\" + Crypto.url_decrypt(model.createdBy) + "\\5\\code";
                var path = "";
                if (model.companyID != "0")
                {
                    if (model.img == "")
                    {
                        path = model.old_img;
                    }
                    else
                    {
                        path = folder + "\\" + model.img;
                    }
                }
                else
                {
                    if (model.img == "")
                    {
                        path = "Default\\Image\\default.png";
                    }
                    else
                    {
                        path = folder + "\\" + model.img;
                    }
                }

                oCmd.CommandText = "company_in_up";
                oCmd.CommandType = CommandType.StoredProcedure;
                oCmd.Parameters.Clear();
                oCmd.Parameters.AddWithValue("@company_id", model.companyID == "0" ? 0 : Crypto.url_decrypt(model.companyID));
                oCmd.Parameters.AddWithValue("@company_code", model.companyCode);
                oCmd.Parameters.AddWithValue("@company_name", model.companyName);
                oCmd.Parameters.AddWithValue("@unit_floor", model.unit);
                oCmd.Parameters.AddWithValue("@building", model.building);
                oCmd.Parameters.AddWithValue("@street", model.street);
                oCmd.Parameters.AddWithValue("@barangay", model.barangay);
                oCmd.Parameters.AddWithValue("@municipality", model.municipality);
                oCmd.Parameters.AddWithValue("@city", model.SelectedCity);
                oCmd.Parameters.AddWithValue("@region", model.SelectedRegion);
                oCmd.Parameters.AddWithValue("@country", model.selectedCompanyCountry);
                oCmd.Parameters.AddWithValue("@zip_code", model.zipCode);
                oCmd.Parameters.AddWithValue("@company_logo", path);
                oCmd.Parameters.AddWithValue("@created_by", Crypto.url_decrypt(model.createdBy));
                //oCmd.Parameters.AddWithValue("@active", model.active);
                SqlDataReader sdr = oCmd.ExecuteReader();
                while (sdr.Read())
                {
                    resp.companyID = Crypto.url_encrypt(sdr["company_id"].ToString());
                    resp.createdBy = Crypto.url_encrypt(sdr["created_by"].ToString());
                    resp.companyCode = sdr["company_code"].ToString();
                    resp.company_series_code = (sdr["series_code"].ToString());
                }
                sdr.Close();
                oTrans.Commit();

                oCmd.CommandText = "create_database";
                oCmd.CommandType = CommandType.StoredProcedure;
                oCmd.Parameters.Clear();
                oCmd.Parameters.AddWithValue("@series_code", resp.company_series_code);
                sdr = oCmd.ExecuteReader();
                while (sdr.Read())
                {
                    resp.company_series_code = (sdr["series_code"].ToString());
                }

                sdr.Close();

                //oCmd.CommandText = "create_database_userdb";
                //oCmd.CommandType = CommandType.StoredProcedure;
                //oCmd.Parameters.Clear();
                //oCmd.Parameters.AddWithValue("@series_code", resp.company_series_code);
                //sdr = oCmd.ExecuteReader();
                //while (sdr.Read())
                //{
                //    resp.company_series_code = (sdr["series_code"].ToString());
                //}

                //sdr.Close();

                //oCmd.CommandText = "create_database_tenantsetupdb";
                //oCmd.CommandType = CommandType.StoredProcedure;
                //oCmd.Parameters.Clear();
                //oCmd.Parameters.AddWithValue("@series_code", resp.company_series_code);
                //sdr = oCmd.ExecuteReader();
                //while (sdr.Read())
                //{
                //    resp.company_series_code = (sdr["series_code"].ToString());
                //}

                //sdr.Close();

                //oCmd.CommandText = "create_database_branchdb";
                //oCmd.CommandType = CommandType.StoredProcedure;
                //oCmd.Parameters.Clear();
                //oCmd.Parameters.AddWithValue("@series_code", resp.company_series_code);
                //sdr = oCmd.ExecuteReader();
                //while (sdr.Read())
                //{
                //    resp.company_series_code = (sdr["series_code"].ToString());
                //}

                //sdr.Close();

                oCmd.CommandText = "create_table";
                oCmd.CommandType = CommandType.StoredProcedure;
                oCmd.Parameters.Clear();
                oCmd.Parameters.AddWithValue("@series_code", resp.company_series_code);
                sdr = oCmd.ExecuteReader();
                while (sdr.Read())
                {
                    resp.company_series_code = (sdr["series_code"].ToString());
                }

                sdr.Close();


            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }
            finally
            {
                oConn.Close();
            }

            return resp;
        }

        public List<CompanyResponse> company_view_sel(string company_id, string created_by)
        {

            List<CompanyResponse> resp = new List<CompanyResponse>();
            string _con = connection._DB_Master;
            DataTable dt = new DataTable();
            SqlConnection oConn = new SqlConnection(_con);
            SqlTransaction oTrans;
            oConn.Open();
            oTrans = oConn.BeginTransaction();
            SqlCommand oCmd = new SqlCommand();
            oCmd.Connection = oConn;
            oCmd.Transaction = oTrans;
            try
            {

                SqlDataAdapter da = new SqlDataAdapter();
                da.SelectCommand = oCmd;
                oCmd.CommandText = "company_view_sel";
                da.SelectCommand.CommandType = CommandType.StoredProcedure;
                oCmd.Parameters.Clear();
                oCmd.Parameters.AddWithValue("@company_id", Crypto.url_decrypt(company_id));
                oCmd.Parameters.AddWithValue("@created_by", Crypto.url_decrypt(created_by));
                da.Fill(dt);
                resp = (from DataRow dr in dt.Rows
                        select new CompanyResponse()
                        {
                            companyID = Crypto.url_encrypt(dr["company_id"].ToString()),
                            companyCode = dr["company_code"].ToString(),
                            createdBy = Crypto.url_encrypt(dr["created_by"].ToString()),
                            street = dr["street"].ToString(),
                            companyName = dr["company_name"].ToString(),
                            barangay = dr["barangay"].ToString(),
                            unit = dr["unit_floor"].ToString(),
                            building = dr["building"].ToString(),
                            municipality = dr["municipality"].ToString(),
                            SelectedCity = Convert.ToInt32(dr["city_id"].ToString()),
                            SelectedRegion = Convert.ToInt32(dr["region_id"].ToString()),
                            selectedCompanyCountry = Convert.ToInt32(dr["country_id"].ToString()),
                            zipCode = dr["zip_code"].ToString(),
                            img = dr["company_logo"].ToString(),
                            old_img = dr["company_logo"].ToString(),
                            active = Convert.ToBoolean(dr["active"].ToString())

                        }).ToList();

                oConn.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }
            finally
            {
                oConn.Close();
            }


            return resp;
        }


        public CompanyInActive company_in_active(string company_id)
        {


            CompanyInActive resp = new CompanyInActive();
            string _con = connection._DB_Master;
            DataTable dt = new DataTable();
            SqlConnection oConn = new SqlConnection(_con);
            SqlTransaction oTrans;
            oConn.Open();
            oTrans = oConn.BeginTransaction();
            SqlCommand oCmd = new SqlCommand();
            oCmd.Connection = oConn;
            oCmd.Transaction = oTrans;
            try
            {
                oCmd.CommandText = "company_in_active";
                oCmd.CommandType = CommandType.StoredProcedure;
                oCmd.Parameters.Clear();
                oCmd.Parameters.AddWithValue("@company_id",Crypto.url_decrypt(company_id));

                SqlDataReader sdr = oCmd.ExecuteReader();
                while (sdr.Read())
                {
                    resp.company_id = sdr["company_id"].ToString();
                }
                sdr.Close();
                oTrans.Commit();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }
            finally
            {
                oConn.Close();
            }

            return resp;
        }



        private string generateJwtToken(AuthenticateResponse user)
        {
            // generate token that is valid for 1 days
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] { new Claim("id", user.id.ToString()) }),
                Expires = DateTime.UtcNow.AddDays(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public AuthenticateResponse GetById(int userId)
        {
            throw new NotImplementedException();
        }


    }
}
