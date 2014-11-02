using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logging_Program
{
    class DatabaseConncter : IDisposable
    {
        private SQLiteConnection databaseConnection;
        private SQLiteCommand commander;
        private string conString;

        public DatabaseConncter(string conString)
        {
            databaseConnection = new SQLiteConnection(conString);
            databaseConnection.Open();
            if (databaseConnection.State != System.Data.ConnectionState.Open)
                throw new Exception("Failed to open the Database");
            commander = databaseConnection.CreateCommand();
            this.conString = conString;
        }


        public List<string> ListTables()
        {
            List<string> tables = new List<string>();
            DataTable asd = databaseConnection.GetSchema("Tables");
            foreach (DataRow item in asd.Rows)
            {
                tables.Add(item[2].ToString());
            }
            return tables;
        }
        public void ExecuteNonQuery(string SQL)
        {
            commander.CommandText = SQL;
            commander.ExecuteNonQuery();
        }
        public DataTable ExecuteQuery(string SQL)
        {
            commander.CommandText = SQL;
            using (SQLiteDataReader reader = commander.ExecuteReader())
            {
                DataTable table = new DataTable();
                table.Load(reader);
                return table;
            }
        }
        public void Dispose()
        {
            databaseConnection.Close();
            databaseConnection.Dispose();
            commander.Dispose();
        }
        public string ConString
        {
            set { conString = value; }
        }
    }
}
