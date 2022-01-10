using System;
using System.Collections.Generic;
using System.Web.Http;
using LP.Helpers;
using Microsoft.Extensions.Caching.Memory;
using static LP.DB.PoolDB;

namespace LP.Pool
{
    public class BlocksController : ApiController
    {
        public IHttpActionResult Get()
        {
            List<BlockData> data = null;

            if (MemCache.Instance.TryGetValue("blocks_data", out data) && data != null)
            {
                return Json(data.ConvertAll(m => new
                {
                    BlockNum = m.BlockNum,
                    TimeStamp = processDateTime(m.TimeStamp),
                    Reward = m.Reward.ToString("F"),
                    Status = m.Status,
                    Miner = m.MinerAddress
                }));
            }

            data = Pool.getMinedBlocks();

            MemCache.Instance.Set("blocks_data", data, new TimeSpan(0, 1, 0));

            return Json(data.ConvertAll(m => new
            {
                BlockNum = m.BlockNum,
                TimeStamp = processDateTime(m.TimeStamp),
                Reward = m.Reward.ToString("F"),
                Status = m.Status,
                Miner = m.MinerAddress
            }));
        }

        private string processDateTime(DateTime dt)
        {
            return dt.ToString("G");
        }
    }
}
