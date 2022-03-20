
using System.Reflection; 

using MyORM.Interfaces;
using MyORM.Objects;

namespace MyORMForMySQL.Objects
{
    public abstract class MySQLContext : DBContext
    {
        protected IDBManager _dBManager;
        public MySQLContext(IDBManager dBManager) : base(dBManager)
        {
            _dBManager = dBManager;

            PropertyInfo[] propertyInfos = this.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(d => d.PropertyType.GetInterfaces().Contains(typeof(IEntityCollection))).ToArray();

            foreach(PropertyInfo propertyInfo in propertyInfos)
            {
                propertyInfo.SetValue(this, Activator.CreateInstance(propertyInfo.PropertyType, _dBManager, this));
            }

        }
    }
}