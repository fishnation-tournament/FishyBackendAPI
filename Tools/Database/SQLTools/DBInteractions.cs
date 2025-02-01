using MySql.Data.MySqlClient;

namespace FishyAPI.Tools.DBInteractions
{
    public class DBConnection
    {

        public string Server { get; set; }
        public string DatabaseName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }

        public MySqlConnection Connection { get; set;}
    
        public bool IsConnect()
        {
            if (Connection != null) return true;
            if (String.IsNullOrEmpty(DatabaseName))
                return false;
            string connstring = string.Format("Server={0}; database={1}; UID={2}; password={3}", Server, DatabaseName, UserName, Password);
            Connection = new MySqlConnection(connstring);
            Connection.Open();

            return true;
        }
    
        public void Close()
        {
            Connection.Close();
        }
    }
}