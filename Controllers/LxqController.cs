using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.IO;

// using AspAPIs.Models;
using Newtonsoft.Json.Linq;
using MySql.Data.MySqlClient;

namespace AspAPIs.Controllers
{

    public class User
    {
        public string UserName {get; set;}
        public string UserId {get; set;}

    }

    [Route("api/[controller]")]
    [ApiController]
    public class LxqController : ControllerBase
    {
         private readonly ILogger _logger;
         public LxqController(ILogger<LxqController> logger)
         {
            _logger = logger;
         }
        // GET api/values
        [HttpGet]
        [EnableCors("AllowAnyOrigin")] 
        public ActionResult<IEnumerable<string>> Get()
        {
            this._logger.LogInformation("gettttttt");
            return new string[] { "value1", "value2" };

        }

        // 注意：型如 Post([FromBody] string user) 的签名会导致客户端返回错误："Unexpected character encountered while parsing value: {. Path '', line 1, position 1."
        //
        // POST api/values
        [HttpPost]
        [EnableCors("AllowAnyOrigin")] 
        public string Post([FromBody] string value)
        {
            try
            {
                this._logger.LogInformation("received post:" + value);
                var jObj = JObject.Parse(value);
                this._logger.LogInformation(jObj[ApiProtocol.Field_CmdType].ToString());
                if (jObj[ApiProtocol.Field_CmdType] == null)
                {
                    return GenerateErrorPackString(Error.no_cmdtype);
                }

                var reqCmd = jObj[ApiProtocol.CmdType_httpreq]?.ToString();
                if (string.IsNullOrEmpty(reqCmd))
                {
                    return GenerateErrorPackString(Error.invalid_httpreq);
                }
                
                return ProcessHttpRequest(jObj, reqCmd);
            }
            catch(Exception ex)
            {
                return GenerateErrorPackString(Error.invalid_pack, "", ex.Message);
            }
        }

        private string ProcessHttpRequest(JObject jObj, string httpReqCmd)
        {
            if (httpReqCmd == ApiProtocol.req_getgames)
            {
                var gameConfFile = System.IO.Path.Combine(AppContext.BaseDirectory, "games.json");
                JArray jGames = null;
                if (System.IO.File.Exists(gameConfFile))
                {
                    string gTexts = System.IO.File.ReadAllText(gameConfFile);
                    jGames = JArray.Parse(gTexts);
                }
                return GenerateSuccessRespPack(httpReqCmd, "games", jGames);
            }
            else if (httpReqCmd == ApiProtocol.req_login)
            {
                string username = jObj[ApiProtocol.Field_UserName].ToString();
                string pwd = jObj[ApiProtocol.Field_Pwd].ToString();
                string sql = string.Format("select userid, username, phone, email from user where password='{0}' and (username='{1}' or email='{1}' or phone='{1}')", pwd, username);
                MySqlCommand cmd = new MySqlCommand(sql, Database.GetDbConnection());
                MySqlDataReader dr = cmd.ExecuteReader();
                if (dr.HasRows)
                {
                    dr.Read();

                    JObject userinfo = new JObject();
                    userinfo["userid"] = dr["userid"].ToString();
                    userinfo["username"] = dr["username"]?.ToString();
                    userinfo["phone"] = dr["phone"]?.ToString();
                    userinfo["email"] = dr["email"]?.ToString();
                    dr.Close();
                    return GenerateSuccessRespPack(httpReqCmd, "login", userinfo);
                }
                else
                {
                    dr.Close();
                    return GenerateErrorPackString(Error.username_or_password_wrong, httpReqCmd);
                }
            }

            return GenerateErrorPackString(Error.invalid_cmd, httpReqCmd);
        }


        private string GenerateErrorPackString(Error error, string errMsg = "")
        {
            var dic = new Dictionary<string, string>();
            dic[ApiProtocol.Field_CmdType] = ApiProtocol.CmdType_httpresp;
            dic[ApiProtocol.Field_Result] = ApiProtocol.Result_Error;
            dic[ApiProtocol.Field_ErrCode] =  ((Int32)error).ToString();
            dic[ApiProtocol.Field_ErrMsg] =  string.IsNullOrEmpty(errMsg) ? Errors.GetErrorMsg(error) : errMsg;
            string jsonStr = JsonConvert.SerializeObject(dic); 
            return jsonStr; 
        }

        private string GenerateErrorPackString(Error error, string reqCmd, string errMsg = "")
        {
            var dic = new Dictionary<string, string>();
            dic[ApiProtocol.Field_CmdType] = ApiProtocol.CmdType_httpresp;
            dic[ApiProtocol.Field_HttpResp] = reqCmd;
            dic[ApiProtocol.Field_Result] = ApiProtocol.Result_Error;
            dic[ApiProtocol.Field_ErrCode] =  ((Int32)error).ToString();
            dic[ApiProtocol.Field_ErrMsg] =  string.IsNullOrEmpty(errMsg) ? Errors.GetErrorMsg(error) : errMsg;
            string jsonStr = JsonConvert.SerializeObject(dic); 
            return jsonStr; 
        }

        private string GenerateSuccessRespPack(string reqCmd)
        {
            var dic = new Dictionary<string, Object>();
            dic[ApiProtocol.Field_CmdType] = ApiProtocol.CmdType_httpresp;
            dic[ApiProtocol.Field_HttpResp] = reqCmd;
            dic[ApiProtocol.Field_Result] = ApiProtocol.Result_OK;
            string jsonStr = JsonConvert.SerializeObject(dic); 
            return jsonStr; 
        }

        private string GenerateSuccessRespPack(string reqCmd, string extraDataName, Object extraData)
        {
            var dic = new Dictionary<string, Object>();
            dic[ApiProtocol.Field_CmdType] = ApiProtocol.CmdType_httpresp;
            dic[ApiProtocol.Field_HttpResp] = reqCmd;
            dic[ApiProtocol.Field_Result] = ApiProtocol.Result_OK;
            dic[ApiProtocol.Field_ResultData] = extraDataName;
            dic[extraDataName] = extraData;
            string jsonStr = JsonConvert.SerializeObject(dic); 
            return jsonStr; 
        }

        private string TestGenerateRespPack()
        {
            var dic1 = new Dictionary<string, object>();
            dic1.Add("Name", "wada");
            dic1.Add("Age", 12);
            var dic2 = new Dictionary<string, object>();
            dic2.Add("Name", "wada1");
            dic2.Add("Age", 123);
            List<Dictionary<string, object>> ls = new List<Dictionary<string, object>>();
            ls.Add(dic1);
            ls.Add(dic2);
            return GenerateSuccessRespPack("get-info", "extra", ls);
        }
        
    }
}
