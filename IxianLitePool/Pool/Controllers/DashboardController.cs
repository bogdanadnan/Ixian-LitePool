using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.Threading.Tasks;
using System.Web.Http;
using LP.Helpers;
using Microsoft.Extensions.Caching.Memory;
using IXICore.Meta;

namespace LP.Pool
{
    public class DashboardData
    {
        public ulong NetworkBlockHeight { get; set; }
        public ulong ActiveMiningBlock { get; set; }
        public int Miners { get; set; }
        public int Workers { get; set; }
        public decimal TotalPayments { get; set; }
        public int PoolHashrate { get; set; }
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

            var activeBlock = Pool.Instance.getActiveBlock();
            int activeMinersCount, activeWorkersCount;
            Miner.getActiveMinersCount(out activeMinersCount, out activeWorkersCount);

            dashboardData = new DashboardData
            {
                NetworkBlockHeight = IxianHandler.getHighestKnownNetworkBlockHeight(),
                ActiveMiningBlock = activeBlock != null ? activeBlock.blockNum : 0,
                Miners = activeMinersCount,
                Workers = activeWorkersCount,
                TotalPayments = Payment.getTotalPayments(),
                PoolHashrate = Pool.getTotalHashrate()
            };

            MemCache.Instance.Set("dashboard_data", dashboardData, new TimeSpan(0, 1, 0));

            return dashboardData;
        }
    }
}
