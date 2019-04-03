using MySql.Data;
using MySql.Data.MySqlClient;
class Database
{
    private static MySqlConnection _dbConn = new MySqlConnection();
    private static string connStr = "server=localhost;user=root;database=SqlTest;port=3306;password=root";

    public static MySqlConnection GetDbConnection()
    {
        if (null == Database._dbConn && Database._dbConn.State != System.Data.ConnectionState.Open)
        {
            try
            {
                var conn = new MySqlConnection(Database.connStr);
                conn.Open();
                Database._dbConn = conn;
            }
            catch(MySqlException ex)
            {
                return null;
            }
        }

        return Database._dbConn;
    }
}