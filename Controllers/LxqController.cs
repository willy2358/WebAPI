using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

// using AspAPIs.Models;
using Newtonsoft.Json.Linq;

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
                var jObj = JObject.Parse(value);
                if (jObj[ApiProtocol.Field_CmdType] == null)
                {
                    return GenerateErrorPackString(Error.Invalid_pack, "");
                }

                return GenerateErrorPackString(Error.Invalid_Cmd, "");
            }
            catch(Exception ex)
            {
                return GenerateErrorPackString(Error.Invalid_pack, "", ex.Message);
            }
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
        
    }
}
