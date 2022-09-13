using  MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyORMForMySQL.Tests.Utils
{
    public static class SQL
    {
        public static void Execute(string sql, string database = "mysql")
        {
            MySqlConnection conn = null;
            try
            {          

                conn = new MySqlConnection($"server=localhost;port=3306;user=root;database={database};password=sup;");

                conn.Open();

                MySqlCommand cmmd = new MySqlCommand(sql, conn);

                cmmd.ExecuteNonQuery();
            }
            catch
            {

            }
            finally
            {
                conn.Close();
            }
        }

        public static bool ExecuteScalar<T>(string sql, out T @out, string database = "mysql")
        {
            MySqlConnection conn = null;
            try
            {
                conn = new MySqlConnection($"server=localhost;port=3306;user=root;database={database};password=sup;");

                conn.Open();

                MySqlCommand cmmd = new MySqlCommand(sql, conn);

                @out = (T)cmmd.ExecuteScalar()!;

                return true;
            }
            catch
            {

            }
            finally
            {
                conn!.Close();
            }

            @out = default(T);

            return false;
        }

        public static void DropDatabase()
        {
            if (SQL.ExecuteScalar<int>($"SELECT 1 FROM information_schema.schemata WHERE SCHEMA_NAME = 'orm_pg_test' ;", out int i) && i == 1)
            {
                SQL.Execute("drop database orm_pg_test;");
            }
        }
    }
}
