using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.Threading.Tasks;
using System.Web.Http;

namespace LP.Pool
{
    public class emp
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Salary { get; set; }
    }

    public class PoolController : ApiController
    {
        emp[] employees = new emp[]
        {
            new emp{Id=0,Name="Morris",Salary="151110"},
            new emp{Id=1,Name="John", Salary="120000"},
            new emp{Id=2,Name="Chris",Salary="140000"},
            new emp{Id=3,Name="Siraj", Salary="90000"}
        };

        public IEnumerable<emp> Get()
        {
            return employees.ToList();
        }

        public emp Get(int Id)
        {
            try
            {
                return employees[Id];
            }
            catch (Exception)
            {
                return new emp();
            }
        }

    }
}
