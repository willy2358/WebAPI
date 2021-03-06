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
using System.Data;

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
        private static string ServiceRootPath = "https://127.0.0.1:44321";
        private static string clientid = "00001";
        private const int room_type_one_table = 1;
        private const int room_type_multi_table = 2;
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
            try
            {
                if (httpReqCmd == ApiProtocol.req_getgames)
                {
                    return ProcessGetGames(httpReqCmd);
                }
                else if (httpReqCmd == ApiProtocol.req_get_roominfo)
                {
                    return ProcessGetRoomInfo(jObj);
                }
                else if (httpReqCmd == ApiProtocol.req_login)
                {
                    return ProcessLogin(jObj, httpReqCmd);
                }
                else if (httpReqCmd == ApiProtocol.req_newroom)
                {
                    return ProcessCreateRoom(jObj);
                }
                else if (httpReqCmd == ApiProtocol.req_new_viproom)
                {
                    return ProcessCreateVipRoom(jObj);
                }

                return GenerateErrorPackString(Error.invalid_cmd, httpReqCmd);
            }
            catch(Exception ex)
            {
                return GenerateErrorPackString(Error.httpserver_inner_error, httpReqCmd, ex.Message);
            }
        }

        private string ProcessCreateVipRoom(JObject jObj)
        {
            try
            {
                Int32 roomid = GetNextRoomId(true);
                if (roomid < 1000)
                {
                    return GenerateErrorPackString(Error.rooms_used_up, ApiProtocol.req_new_viproom);
                }
                int table_limits = 1;
                string userid = jObj[ApiProtocol.Field_UserId].ToString();
                string room_vis_type = jObj[ApiProtocol.Field_Room_Visible_Type].ToString();

                string sql = "insert into room_vip(room_no,userid,tables_limit, visible_type, expired) "
                            +$" values({roomid},{userid},{table_limits},{room_vis_type},0)";
                
                MySqlCommand cmd = new MySqlCommand(sql, Database.GetDbConnection());
                cmd.ExecuteNonQuery();
                var room = "room";
                JObject jRoomInfo = new JObject();
                jRoomInfo["roomid"] = roomid;
                jRoomInfo["roomtype"] = room_type_multi_table; 
                jRoomInfo["roomtoken"] = CreateSessionToken();
                return GenerateSuccessRespPack(ApiProtocol.req_newroom, room, jRoomInfo);
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        private string ProcessCreateRoom(JObject jObj)
        {
            try
            {
                Int32 roomid = GetNextRoomId(false);
                if (roomid < 1000)
                {
                    return GenerateErrorPackString(Error.rooms_used_up, ApiProtocol.req_newroom);
                }

                string userid = jObj[ApiProtocol.Field_UserId].ToString();
                string gameid = jObj[ApiProtocol.Field_GameId].ToString();
                string rounds = jObj[ApiProtocol.Field_RoundNum].ToString();
                string room_fee_stuffid = jObj[ApiProtocol.Field_FeeStuffId].ToString();
                string stake_stuffid = jObj[ApiProtocol.Field_Stake_StuffId].ToString();
                string stake_base_num = jObj[ApiProtocol.Field_Stake_Base_Num].ToString();
                string fee_charge_type = jObj[ApiProtocol.Field_FeeChargeType].ToString();
                string room_vis_type = jObj[ApiProtocol.Field_Room_Visible_Type].ToString();

                string sql = "insert into room(room_no,userid,gameid,round_num,ex_ip_cheat,ex_gps_cheat,fee_stuff_id,stake_stuff_id,"
                            +$"stake_base_score,fee_charge_type,visible_type) "
                            +$" values({roomid},{userid},{gameid},{rounds},0, 0, {room_fee_stuffid},{stake_stuffid},{stake_base_num},{fee_charge_type},{room_vis_type})";
                
                MySqlCommand cmd = new MySqlCommand(sql, Database.GetDbConnection());
                cmd.ExecuteNonQuery();

                var room = "room";
                JObject jRoomInfo = new JObject();
                jRoomInfo["roomid"] = roomid;
                jRoomInfo["gameid"] = gameid;
                jRoomInfo["roomtype"] = room_type_one_table;  
                jRoomInfo["roomtoken"] = CreateSessionToken();
                return GenerateSuccessRespPack(ApiProtocol.req_newroom, room, jRoomInfo);
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        //ToDo support out isUsedUp
        private Int32 GetNextRoomId(bool isVip = false)
        {
            try
            {
                var procName = isVip? "next_vip_roomid":"next_roomid";
                MySqlCommand mysqlCommand = new MySqlCommand(procName, Database.GetDbConnection());
                mysqlCommand.CommandType = CommandType.StoredProcedure;
                MySqlParameter out_roomid = new MySqlParameter("@p_out",MySqlDbType.Int32);
                mysqlCommand.Parameters.Add(out_roomid);
                out_roomid.Direction = ParameterDirection.Output;
                mysqlCommand.ExecuteNonQuery();
                Int32 roomid = Convert.ToInt32(out_roomid.Value);
                return roomid;
            }
            catch(MySqlException ex)
            {
                throw ex;
            }
        }

        private string ProcessLogin(JObject jObj, string httpReqCmd)
        {
            string username = jObj[ApiProtocol.Field_UserName].ToString();
            string pwd = jObj[ApiProtocol.Field_Pwd].ToString();
            string sql = string.Format("select userid, username, image, alias, phone, email from user where password='{0}' and (username='{1}' or email='{1}' or phone='{1}')", pwd, username);
            MySqlCommand cmd = new MySqlCommand(sql, Database.GetDbConnection());
            // MySqlDataReader dr = cmd.ExecuteReader();
            
            MySqlDataAdapter adapter = new MySqlDataAdapter();
            adapter.SelectCommand = cmd;
            DataSet ds = new DataSet();
            adapter.Fill(ds);
            if (ds.Tables.Count >= 0 && ds.Tables[0].Rows.Count > 0)
            {
                UInt32 userid = Convert.ToUInt32(ds.Tables[0].Rows[0]["userid"]);
                JObject jLogin = new JObject();

                jLogin["token"] =  CreateSessionToken();

                var img = ds.Tables[0].Rows[0]["image"]?.ToString();
                JObject jUserInfo = new JObject();
                jUserInfo["userid"] = userid.ToString();
                jUserInfo["alias"] = ds.Tables[0].Rows[0]["alias"]?.ToString();
                jUserInfo["username"] = ds.Tables[0].Rows[0]["username"]?.ToString();
                jUserInfo["phone"] = ds.Tables[0].Rows[0]["phone"]?.ToString();
                jUserInfo["email"] = ds.Tables[0].Rows[0]["email"]?.ToString();
                jUserInfo["image"] = string.IsNullOrEmpty(img)? "" : System.IO.Path.Combine(ServiceRootPath, img.ToString());
                jUserInfo["stuffs"] = QueryUserStuffs(userid);
                jLogin["user-info"] = jUserInfo;

                // var sig = CryptoHelper.SignDataToBase64Str(salt);
                return GenerateSuccessRespPack(httpReqCmd, "login", jLogin);
            }
            else
            {
                return GenerateErrorPackString(Error.username_or_password_wrong, httpReqCmd);
            }
        }

        private JObject CreateSessionToken()
        {
            var salt = System.Guid.NewGuid().ToString();
            JObject jToken = new JObject();
            jToken["clientid"] = clientid;
            jToken["salt"] = salt;
            jToken["signature"] = CryptoHelper.SignDataToBase64Str(salt);
            jToken["expire"] = DateTime.Now.AddDays(2).ToString("yyyy-MM-dd HH:mm:ss");
            return jToken;
        }
        private string ProcessGetGames(string httpReqCmd)
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

        private string ProcessGetRoomInfo(JObject jObj)
        {
            try
            {
                int roomid =  Convert.ToInt32(jObj[ApiProtocol.Field_RoomId]);
                if (roomid < 20000)
                {
                    //reserved vip room
                    string sql = $"select room_no, userid from room_vip where room_no = {roomid}";
                    MySqlCommand cmd = new MySqlCommand(sql, Database.GetDbConnection());
                    MySqlDataAdapter adapter = new MySqlDataAdapter();
                    adapter.SelectCommand = cmd;
                    DataSet ds = new DataSet();
                    adapter.Fill(ds);
                    if (ds.Tables.Count >= 0 && ds.Tables[0].Rows.Count > 0)
                    {
                        UInt32 ownerid = Convert.ToUInt32(ds.Tables[0].Rows[0]["userid"]);
                        JObject jRoom = new JObject();
                        jRoom["ownerid"] = ownerid;
                        jRoom["roomtype"] = room_type_multi_table;
                        jRoom["roomtoken"] = CreateSessionToken();
                        return GenerateSuccessRespPack(ApiProtocol.req_get_roominfo, "room", jRoom);
                    }
                    else
                    {
                        return GenerateErrorPackString(Error.invalid_roomid, ApiProtocol.req_get_roominfo);
                    }
                }
                else
                {
                    //ordinary room
                    string sql = $"select room_no, userid, gameid, round_num, fee_stuff_id, stake_stuff_id,"
                                +$"stake_base_score,fee_charge_type from room where room_no = {roomid}";
                    MySqlCommand cmd = new MySqlCommand(sql, Database.GetDbConnection());
                    MySqlDataAdapter adapter = new MySqlDataAdapter();
                    adapter.SelectCommand = cmd;
                    DataSet ds = new DataSet();
                    adapter.Fill(ds);
                    if (ds.Tables.Count >= 0 && ds.Tables[0].Rows.Count > 0)
                    {
                        UInt32 ownerid = Convert.ToUInt32(ds.Tables[0].Rows[0]["userid"]);
                        JObject jRoom = new JObject();
                        jRoom["ownerid"] = ownerid;
                        jRoom["roomtype"] = room_type_one_table;
                        jRoom["roomtoken"] = CreateSessionToken();
                        jRoom["gameid"] = Convert.ToUInt32(ds.Tables[0].Rows[0]["gameid"]);
                        jRoom[ApiProtocol.Field_RoundNum] = Convert.ToUInt32(ds.Tables[0].Rows[0]["round_num"]);
                        jRoom[ApiProtocol.Field_FeeStuffId] = Convert.ToUInt32(ds.Tables[0].Rows[0]["fee_stuff_id"]);
                        jRoom[ApiProtocol.Field_Stake_StuffId] = Convert.ToUInt32(ds.Tables[0].Rows[0]["stake_stuff_id"]);
                        jRoom[ApiProtocol.Field_Stake_Base_Num] = Convert.ToUInt32(ds.Tables[0].Rows[0]["stake_base_score"]);
                        jRoom[ApiProtocol.Field_FeeChargeType] = Convert.ToUInt32(ds.Tables[0].Rows[0]["fee_charge_type"]);
                       
                        return GenerateSuccessRespPack(ApiProtocol.req_get_roominfo, "room", jRoom);
                    }
                    else
                    {
                        return GenerateErrorPackString(Error.invalid_roomid, ApiProtocol.req_get_roominfo);
                    }
                }
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        //private string 

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

        private JArray QueryUserStuffs(UInt32 userid)
        {
            try
            {
                string sql = string.Format(@"select b.stuffid, b.stuffname, a.amount from user_stuff  as a, stuff as b
                                            where a.userid = {0}
                                            and a.stuffid = b.stuffid;", userid);
                MySqlCommand cmd = new MySqlCommand(sql, Database.GetDbConnection());
                MySqlDataAdapter adapter = new MySqlDataAdapter();
                adapter.SelectCommand = cmd;
                DataSet ds = new DataSet();
                adapter.Fill(ds);
                if (ds.Tables == null || ds.Tables.Count < 1 || null == ds.Tables[0].Rows || ds.Tables[0].Rows.Count < 1)
                {
                    return null;
                }

                JArray arr = new JArray();
                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    JObject jObj = new JObject();
                    jObj["stuffid"] = Convert.ToUInt16(row["stuffid"]);
                    jObj["name"] = row["stuffname"].ToString();
                    jObj["amount"] = Convert.ToUInt64(row["amount"]);
                    arr.Add(jObj);
                }

                return arr;
            }
            catch(Exception)
            {
                return null;
            }
        }
        
    }
}
