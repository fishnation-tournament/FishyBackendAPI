using MySql.Data.MySqlClient;

namespace FishyAPI.Tools
{
    public class SQLInteraction
    {
        private MySqlConnection dbConn;

        public struct User
        {
            public ulong UID;
            public ulong OptBLUID;
            public string Username;
            public string UserPfpLink;
            public string UserBio;
            public ulong DiscordID;
            public string DiscordUsername;
            public DateTime RegistrationDate;
            public string Role;
        }
        
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
    