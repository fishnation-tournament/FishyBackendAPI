using MySql.Data.MySqlClient;

namespace FishyAPI.Tools.DBInteractions
{
    public class DBConnection : IDisposable
    {
        public MySqlConnection Connection { get; }
        private bool disposed = false;

        public DBConnection(string Server, string DatabaseName, string UserName, string Password)
        {
            string connstring = string.Format("Server={0}; database={1}; UID={2}; password={3}", Server, DatabaseName, UserName, Password);
            Connection = new MySqlConnection(connstring);
        }

        public void Open()
        {
            Connection.Open();
        }

        public void Close()
        {
            Connection.Close();
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    Connection.Dispose();
                }

                disposed = true;
            }
        }

        ~DBConnection()
        {
            Dispose(false);
        }
    }
}