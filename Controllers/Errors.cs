

public enum Error 
{
    undefined = -1,
    OK = 0,
    Invalid_pack,
    Invalid_Cmd,

}

public class Errors
{
    public static string GetErrorMsg(Error error)
    {
        switch(error)
        {
            case Error.OK: return "OK";
            case Error.Invalid_pack: return "invalid message package";
            case Error.Invalid_Cmd: return "Invalid command";
            default : return "undefinded error";
        }
    }
}