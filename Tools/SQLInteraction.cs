using MySql.Data.MySqlClient;

namespace FishyAPI.Tools
{
    public class SQLInteraction
    {
        private MySqlConnection dbConn;
        
        public SQLInteraction(MySqlConnection dbConn)
        {
            this.dbConn = dbConn;
        }
        
        public void SendCommand(string query)
        {
            var cmd = new MySqlCommand(query, dbConn);
            cmd.ExecuteNonQuery();
            
        }

        public MySqlDataReader GetReader(string query)
        {
            var cmd = new MySqlCommand(query, dbConn);
            MySqlDataReader reader = cmd.ExecuteReader();
            
            return reader;
        }
    }
}
    