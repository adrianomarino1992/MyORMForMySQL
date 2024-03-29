﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Data;
using System.Text.Json;


using MyORM.Attributes;
using MyORM.Exceptions;
using MyORM.Interfaces;
using MyORM.Enums;


namespace MyORMForMySQL.Objects
{

    public class MySQLManager : IDBManager
    {
        public MySQLConnectionBuilder MySQLConnectionBuilder { get; }

        public MySQLManager(MySQLConnectionBuilder builder)
        {
            MySQLConnectionBuilder = builder;
        }

        public void CreateColumn(string table, PropertyInfo info)
        {

            bool createColumn = info.GetCustomAttribute<DBIgnoreAttribute>() == null;

            if (!createColumn)
                return;

            bool primaryKey = info.GetCustomAttribute<DBPrimaryKeyAttribute>() != null;


            if (primaryKey && info.PropertyType != typeof(long))
                throw new InvalidTypeException($"The type of a primary key must be {typeof(long).Name}");


            bool foreignKey = info.GetCustomAttribute<DBForeignKeyAttribute>() != null;

            if (foreignKey && info.PropertyType != typeof(long))
                throw new InvalidTypeException($"The type of a foreign key must be {typeof(long).Name}");


            bool isArray = info.PropertyType.IsAssignableTo(typeof(IEnumerable)) && info.PropertyType != typeof(string);

            string colName, colType = String.Empty;


            if (isArray)
            {

                Type arrayType = info.PropertyType.GetElementType() ?? info.PropertyType.GetGenericArguments()[0];

                bool isValueType = (!arrayType.IsClass) || arrayType == typeof(string);

                if (isValueType)
                {
                    try
                    {
                        colName = info.GetCustomAttribute<DBColumnAttribute>()?.Name ?? info.Name.ToLower();
                        colType = GetDBTypeFromStruct(arrayType);
                    }
                    catch
                    {
                        return;
                    }

                    if (ExecuteScalar<int>($"SELECT 1 FROM information_schema.columns WHERE table_schema = '{MySQLConnectionBuilder.DataBase}' AND table_name = '{table}' AND column_name = '{colName}'") == 1)
                        return;
                    ExecuteScalar<int>($"ALTER TABLE {MySQLConnectionBuilder.Schema}.{table} ADD COLUMN {colName} text ");
                    return;
                }

            }

            try
            {
                (colName, colType) = GetColumnNameAndType(info);
            }
            catch
            {
                return;
            }


            primaryKey = primaryKey && (colType.Trim() == "integer" || colType.Trim() == "bitint" || colType.Trim() == "integer auto_increment" || colType.Trim() == "bigint auto_increment");

            if (ExecuteScalar<int>($"SELECT 1 FROM information_schema.columns WHERE table_schema = '{MySQLConnectionBuilder.DataBase}' AND table_name = '{table}' AND column_name = '{colName}'") == 1)
                return;
            ExecuteScalar<int>($"ALTER TABLE {MySQLConnectionBuilder.Schema}.{table} ADD COLUMN `{colName}` {colType} {(primaryKey ? " NOT NULL PRIMARY KEY " : "")}");

            if (primaryKey)
                return;

            PropertyInfo? foreingKeyType = info.ReflectedType?.GetProperties(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(d => d.Name == info.Name.Replace("Id", ""));
            PropertyInfo? subKeyProperty = info.ReflectedType?.GetProperties(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(d => d.GetCustomAttribute<DBPrimaryKeyAttribute>() != null);

            if (subKeyProperty != null && foreingKeyType != null && foreignKey)
            {
                string subTable = foreingKeyType.PropertyType.GetCustomAttribute<DBTableAttribute>()?.Name ?? foreingKeyType.PropertyType.Name.ToLower();
                string subColName = subKeyProperty.GetCustomAttribute<DBColumnAttribute>()?.Name ?? subKeyProperty.Name.ToLower();
                DeleteMode mode = info.GetCustomAttribute<DBDeleteModeAttribute>()?.DeleteMode ?? DeleteMode.NOACTION;
                string consName = $"{table}_{colName}_fkey";
                string constraint = $@"ALTER TABLE {MySQLConnectionBuilder.Schema}.{table} ADD CONSTRAINT {consName} FOREIGN KEY ({colName})
                                        REFERENCES {MySQLConnectionBuilder.Schema}.{subTable} ({subColName}) 
                                        ON UPDATE NO ACTION
                                        ON DELETE {(mode == DeleteMode.CASCADE ? "CASCADE" : "NO ACTION")}";

                if (ExecuteScalar<int>($"SELECT 1 FROM information_schema.REFERENTIAL_CONSTRAINTS  WHERE CONSTRAINT_NAME   = '{table}' ") == 1)
                    return;

                ExecuteNonQuery(constraint);

            }
        }

        public (string, string) GetColumnNameAndType(PropertyInfo info, bool checkKey = true)
        {
            string colName = info.GetCustomAttribute<DBColumnAttribute>()?.Name ?? info.Name.ToLower();
            bool key = info.GetCustomAttribute<DBPrimaryKeyAttribute>() != null;

            if (key && checkKey)
            {
                if (info.PropertyType == typeof(Int32))
                    return (colName, " integer auto_increment ");
                if (info.PropertyType == typeof(long))
                    return (colName, " bigint auto_increment ");
            }


            if (info.PropertyType == typeof(string))
                return (colName, " text ");

            if (info.PropertyType == typeof(Int32))
                return (colName, " integer ");

            if (info.PropertyType == typeof(long))
                return (colName, " bigint ");

            if (info.PropertyType == typeof(double) || info.PropertyType == typeof(float))
                return (colName, " real ");

            if (info.PropertyType == typeof(DateTime))
                return (colName, " date ");

            if (info.PropertyType == typeof(bool))
                return (colName, " boolean ");

            if (info.PropertyType.IsEnum)
                return (colName, " integer ");

            throw new CastFailException($"Can not cast the property {info.Name} to a column");

        }


        public string GetDBTypeFromStruct(Type type)
        {
            if (type == typeof(string))
                return " text ";

            if (type == typeof(Int32))
                return " integer ";

            if (type == typeof(long))
                return " bigint ";

            if (type == typeof(double) || type == typeof(float))
                return " real ";

            if (type == typeof(DateTime))
                return " date ";

            if (type == typeof(bool))
                return " boolean ";

            if (type.IsEnum)
                return " integer ";

            throw new CastFailException($"Can not cast the type {type.Name} to a DBType");


        }

        public void CreateDataBase()
        {
            if (!DataBaseExists())
            {
                ExecuteNonQuery($"CREATE DATABASE IF NOT EXISTS {MySQLConnectionBuilder.DataBase.ToLower().Trim()}", DB.MYSQL);
            }
        }

        public void CreateTable<T>()
        {

            string tableName = typeof(T).GetCustomAttribute<DBTableAttribute>()?.Name ?? typeof(T).Name.ToLower();

            ExecuteNonQuery($"CREATE TABLE IF NOT EXISTS {MySQLConnectionBuilder.Schema}.{tableName}(objid integer)");

        }

        public bool ColumnExists(string table, string colName)
        {
            return ExecuteScalar<int>($"SELECT * FROM information_schema.columns WHERE table_catalog = '{MySQLConnectionBuilder.DataBase}' AND table_name = '{table}' AND column_name = '{colName}'") == 1;
        }

        public bool DataBaseExists()
        {
            return ExecuteScalar<int>($"SELECT 1 FROM information_schema.schemata WHERE schema_name='{MySQLConnectionBuilder.DataBase.ToLower().Trim()}'", DB.MYSQL) == 1;
        }


        public bool TableExists<T>()
        {
            string tableName = typeof(T).GetCustomAttribute<DBTableAttribute>()?.Name ?? typeof(T).Name.ToLower();

            return ExecuteScalar<int>($"SELECT * FROM information_schema.tables WHERE table_catalog = '{MySQLConnectionBuilder.DataBase}' AND table_name = '{tableName}'") == 1;
        }


        public void DropColumn(string table, PropertyInfo info)
        {
            string colName = info.GetCustomAttribute<DBColumnAttribute>()?.Name ?? info.Name.ToLower();

            if (ColumnExists(table, colName))
            {
                ExecuteNonQuery($"ALTER TABLE {table} DROP COLUMN {colName}");
            }
        }

        public void DropDataBase()
        {
            if (DataBaseExists())
            {
                ExecuteNonQuery($"DROP DATABASE {MySQLConnectionBuilder.DataBase.ToLower().Trim()}", DB.MYSQL);
            }
        }

        public void DropTable<T>()
        {
            string tableName = typeof(T).GetCustomAttribute<DBTableAttribute>()?.Name ?? typeof(T).Name.ToLower();

            ExecuteNonQuery($"DROP TABLE IF EXISTS {tableName}");
        }

        public void FitColumns(string table, IEnumerable<PropertyInfo> infos)
        {
            DataSet? dt = GetDataSet($"SELECT column_name, data_type FROM information_schema.columns WHERE table_schema = '{MySQLConnectionBuilder.DataBase}' AND table_name = '{table}'");

            List<(string?, string?)> columns = new List<(string?, string?)>();

            if (dt == null)
                return;

            if (dt.Tables.Count == 0 || dt.Tables[0].Rows.Count == 0)
                return;

            foreach (DataRow row in dt.Tables[0].Rows)
            {
                columns.Add((row["column_name"].ToString(), row["data_type"].ToString()));
            }

            foreach ((string? col, string? type) in columns)
            {
                PropertyInfo? info = infos.FirstOrDefault(d => d.GetCustomAttribute<DBColumnAttribute>()?.Name == col || (d.GetCustomAttribute<DBColumnAttribute>() == null && col == d.Name.ToLower()));
                if (col != null)
                {
                    if (info == null)
                    {
                        ExecuteNonQuery($"ALTER TABLE {MySQLConnectionBuilder.Schema}.{table} DROP COLUMN {col}");
                        continue;
                    }
                }


                if (info != null)
                {
                    bool isArray = info.PropertyType.IsAssignableTo(typeof(IEnumerable)) && info.PropertyType != typeof(string);

                    string colType = String.Empty;

                    if (isArray)
                    {
                        Type arrayType = info.PropertyType.GetElementType() ?? info.PropertyType.GetGenericArguments()[0];

                        bool isValueType = (!arrayType.IsClass) || arrayType == typeof(string);

                        if (isValueType)
                        {
                            colType = GetDBTypeFromStruct(typeof(string));
                            goto UP;
                        }

                        continue;

                        
                    }

                    (string _, colType) = GetColumnNameAndType(info, false);

                UP:
                    if (colType.Trim().ToLower() != type?.Trim().ToLower())
                    {
                        try
                        {
                            ExecuteNonQuery($"ALTER TABLE {MySQLConnectionBuilder.Schema}.{table} MODIFY {col} {colType}");

                        }
                        catch
                        {
                            ExecuteNonQuery($"ALTER TABLE {MySQLConnectionBuilder.Schema}.{table} DROP COLUMN {col}");
                            CreateColumn(table, info);

                        }
                        continue;
                    }

                }


            }

        }

        public bool TryConnection()
        {
            IDbConnection conn = MySQLConnectionBuilder.NewConnection();

            try
            {
                conn.Open();
            }
            catch
            {

                return false;
            }
            finally
            {

                conn.Close();
            }

            return true;
        }

        public T? ExecuteScalar<T>(string query, DB db = DB.BUILDER)
        {
            string temp = String.Empty;

            if (db == DB.MYSQL)
            {
                temp = MySQLConnectionBuilder.DataBase;
                MySQLConnectionBuilder.DataBase = "mysql";
            }

            IDbConnection conn = MySQLConnectionBuilder.NewConnection();

            if (db == DB.MYSQL)
                MySQLConnectionBuilder.DataBase = temp;

            try
            {
                conn.Open();

                IDbCommand cmd = MySQLConnectionBuilder.NewCommand(conn);

                cmd.CommandText = query;

                object? r = cmd.ExecuteScalar();

                if (r == null)
                    return default(T);

                return (T)r;
            }
            catch (Exception ex)
            {
                throw new QueryFailException(ex.Message);
            }
            finally
            {
                conn.Close();
            }
        }

        public void ExecuteNonQuery(string query, DB db = DB.BUILDER)
        {
            string temp = String.Empty;

            if (db == DB.MYSQL)
            {
                temp = MySQLConnectionBuilder.DataBase;
                MySQLConnectionBuilder.DataBase = "mysql";
            }

            IDbConnection conn = MySQLConnectionBuilder.NewConnection();

            if (db == DB.MYSQL)
                MySQLConnectionBuilder.DataBase = temp;

            try
            {
                conn.Open();

                IDbCommand cmd = MySQLConnectionBuilder.NewCommand(conn);

                cmd.CommandText = query;

                cmd.ExecuteNonQuery();

            }
            catch (Exception ex)
            {
                throw new QueryFailException(ex.Message);
            }
            finally
            {
                conn.Close();
            }
        }

        public DataSet GetDataSet(string query, DB db = DB.BUILDER)
        {
            string temp = String.Empty;

            if (db == DB.MYSQL)
            {
                temp = MySQLConnectionBuilder.DataBase;
                MySQLConnectionBuilder.DataBase = "mysql";
            }

            IDbConnection conn = MySQLConnectionBuilder.NewConnection();

            if (db == DB.MYSQL)
                MySQLConnectionBuilder.DataBase = temp;

            try
            {
                conn.Open();

                IDbCommand cmd = MySQLConnectionBuilder.NewCommand(conn);

                cmd.CommandText = query;

#pragma warning disable CS8604 // Possível argumento de referência nula.
                IDataAdapter? r = new MySql.Data.MySqlClient.MySqlDataAdapter(cmd as MySql.Data.MySqlClient.MySqlCommand);
#pragma warning restore CS8604 // Possível argumento de referência nula.

                DataSet ds = new DataSet();

                r.Fill(ds);

                return ds;

            }
            catch (Exception ex)
            {
                throw new CastFailException(ex.Message);
            }
            finally
            {
                conn.Close();
            }
        }


    }

    public enum DB
    {
        MYSQL = 0,
        BUILDER = 1

    }

}
