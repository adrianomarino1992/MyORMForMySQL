﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyORM.Attributes;

namespace MyORMForMySQL.Tests.Classes
{
    public class MySQLContext : MyORMForMySQL.Objects.MySQLContext
    {
        public MySQLContext(MyORMForMySQL.Objects.MySQLManager manager) : base(manager)
        {
             
        }

        public MySQLCollection<Seller> Sellers { get; set; }
        public MySQLCollection<Departament> Departaments { get; set; }
        public MySQLCollection<Sale> Sales { get; set; }
        public MySQLCollection<Product> Products { get; set; }

    }


    public class Seller : Entity
    {
        public string Name { get; set; }

        public string Email { get; set; }

        public Departament Departament { get; set; }

        [DBForeignKey]
        public long DepartamentId { get; set; }

        public List<Sale> Sales { get; set; }

        public List<string> Phones { get; set; } 

    }



    public class Departament : Entity
    {
        public string Name { get; set; }
    }

    public class Sale : Entity
    { 
        public int Quantity { get; set; }

        public Product Product { get; set; }

        [DBForeignKey]
        public long ProductId { get; set; }

        public Seller Seller { get; set; }

        [DBForeignKey]
        public long SellerId { get; set; }
    }


    public class Product : Entity
    {
        public string Name { get; set; }

        public double Value { get; set; }
    }

    public abstract class Entity
    {
        [DBPrimaryKey]
        public long Id { get; set; }
    }

}
