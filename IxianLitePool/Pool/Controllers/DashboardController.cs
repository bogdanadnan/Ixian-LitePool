using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;
using LP.Helpers;
using Microsoft.Extensions.Caching.Memory;
using IXICore.Meta;
using System.Net.Http;

namespace LP.Pool
{
    public class DashboardData
    {
        public ulong NetworkBlockHeight { get; set; }
        public ulong ActiveMiningBlock { get; set; }
        public int Miners { get; set; }
        public int Workers { get; set; }
        public decimal TotalPayments { get; set; }
        public decimal TotalPending { get; set; }
        public int PoolHashrate { get; set; }
        public ulong PoolDifficulty { get; set; }
        public int BlocksMined { get; set; }
        public decimal IxiPrice { get; set; }
    }

    public class DashboardController : ApiController
    {
        public IHttpActionResult Get()
        {
            DashboardData dashboardData = null;

            if (MemCache.Instance.TryGetValue("dashboard_data", out dashboardData) && dashboardData != null)
            {
                return Json(dashboardData);
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
                TotalPending = (decimal)IxianHandler.getWalletBalance(new byte[0]).getAmount() / 100000000,
                PoolHashrate = Pool.getTotalHashrate(),
                PoolDifficulty = Pool.Instance.getDifficulty(),
                BlocksMined = Pool.getBlocksMinedInLast24h(),
                IxiPrice = get1000IxiPrice()
            };

            MemCache.Instance.Set("dashboard_data", dashboardData, new TimeSpan(0, 1, 0));

            return Json(dashboardData);
        }

        private decimal get1000IxiPrice()
        {
            return IxiPrice.Instance.getIxiPrice() * 1000;
        }
    }
}
