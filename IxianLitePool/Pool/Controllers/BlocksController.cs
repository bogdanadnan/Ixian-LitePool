using System;
using System.Collections.Generic;
using System.Web.Http;
using static LP.DB.PoolDB;

namespace LP.Pool
{
    public class BlocksController : ApiController
    {
        public IHttpActionResult Get()
        {
            List<BlockData> data = Pool.getMinedBlocks();

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
