

public enum Error 
{
    undefined = -1,
    OK = 0,

    no_cmdtype,
    invalid_pack,
    invalid_cmd,
    invalid_httpreq,

    username_or_password_wrong,

    unsupported_cmd,

    


}

public class Errors
{
    public static string GetErrorMsg(Error error)
    {
        switch(error)
        {
            case Error.OK: return "OK";
            case Error.no_cmdtype: return "lacking the cmdtype field";
            case Error.invalid_pack: return "invalid message package";
            case Error.invalid_cmd: return "invalid command";
            case Error.invalid_httpreq: return "invalid httpreq command";
            case Error.username_or_password_wrong: return "username or password is wrong";
            default : return "undefinded error";
        }
    }
}