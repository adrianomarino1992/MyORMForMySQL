using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyORM.Enums;
using MyORM.Interfaces;

using MySql.Data;

namespace MyORMForMySQL.Objects
{
    public class MySQLConnectionBuilder : IDBConnectionBuilder
    {
        public MySQLConnectionBuilder(string user, string password, int port = 3306, string host = "127.0.0.1", string dataBase = "mysql")
        {
            User = user;
            Password = password;
            Port = port;
            Host = host;
            DataBase = dataBase;
            Schema = dataBase;
            ProviderType = ProviderType.MYSQL;
        }

        public string User { get; set; }
        public string Password { get; set; }
        public int Port { get; set; }
        public string Host { get; set; }
        public string DataBase { get; set; }
        public string Schema { get; set; }
        public ProviderType ProviderType { get; set; }

        public IDbCommand NewCommand(IDbConnection conn)
        {
            return new MySql.Data.MySqlClient.MySqlCommand("", conn as MySql.Data.MySqlClient.MySqlConnection);
        }

        public IDbConnection NewConnection()
        {            
            return new MySql.Data.MySqlClient.MySqlConnection($"server={Host};user={User};database={DataBase};password={Password};port={Port};");
        }
    }
}
