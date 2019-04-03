using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

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
            this._logger.LogInformation("ppoooost");
            try
            {
                Console.WriteLine(value);
                var jObj = JObject.Parse(value);
                Console.WriteLine(jObj["UserName"]);
            }
            catch(Exception ex)
            {
                
            }

            // return value;
            return "hello";
        }


        
    }
}
