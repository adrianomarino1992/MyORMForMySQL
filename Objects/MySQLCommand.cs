using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyORM.Enums;
using MyORM.Interfaces;

namespace MyORMForMySQL.Objects
{
    public class MySQLCommand : ICommand
    {
        private string? _sql;
        public ProviderType ProviderType { get => ProviderType.MYSQL; set => _=value; }

        public string Sql()
        {
            return _sql ?? String.Empty;
        }

        public MySQLCommand(string sql)
        {
            _sql = sql;
        }
    }
}
