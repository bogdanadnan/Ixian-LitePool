using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.Threading.Tasks;
using System.Web.Http;

namespace LP.Pool
{
    public class Pool
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Salary { get; set; }
    }

    public class PoolController : ApiController
    {
        Pool[] employees = new Pool[]
        {
            new Pool{Id=0,Name="Morris",Salary="151110"},
            new Pool{Id=1,Name="John", Salary="120000"},
            new Pool{Id=2,Name="Chris",Salary="140000"},
            new Pool{Id=3,Name="Siraj", Salary="90000"}
        };

        public IEnumerable<Pool> Get()
        {
            return employees.ToList();
        }

        public Pool Get(int Id)
        {
            try
            {
                return employees[Id];
            }
            catch (Exception)
            {
                return new Pool();
            }
        }

    }
}
