using MySql.Data.MySqlClient;

namespace MyWebApp.Database;

class DbService : IDisposable
{
    public MySqlConnection? connection { private set; get; }
    public DbService(string connectionString)
    {
        connection = new MySqlConnection(connectionString);
        connection.Open();
    }

    public void Dispose() => connection?.Close(); 

    public async Task<string> GetData()
    {
        MySqlDataReader? sqlDataReader = null;
        string data = $"\n\t[ NAME ]\t\t[ AGE ]\n\n";
        try
        {
            MySqlCommand sqlCommand = new MySqlCommand("select * from users", this.connection);
            sqlDataReader = await Task.Run(() => sqlCommand.ExecuteReader());

            while (sqlDataReader.Read())
            {
                data += $"\t{sqlDataReader["Name"]}\t\t\t{sqlDataReader["Age"]}\n";
            }
        }
        finally
        {
            sqlDataReader?.Close();
            
        }
        return data;
    }

}

