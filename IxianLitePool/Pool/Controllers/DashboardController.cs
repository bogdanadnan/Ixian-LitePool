using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.Threading.Tasks;
using System.Web.Http;
using LP.Helpers;
using Microsoft.Extensions.Caching.Memory;

namespace LP.Pool
{
    public class DashboardData
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Salary { get; set; }
    }

    public class DashboardController : ApiController
    {
        public DashboardData Get()
        {
            DashboardData dashboardData = null;

            if (MemCache.Instance.TryGetValue("dashboard_data", out dashboardData) && dashboardData != null)
            {
                return dashboardData;
            }

            dashboardData = new DashboardData { Id = 0, Name = "test name", Salary = "test salary" };
            MemCache.Instance.Set("dashboard_data", dashboardData, new TimeSpan(0, 1, 0));

            return dashboardData;
        }
    }
}
