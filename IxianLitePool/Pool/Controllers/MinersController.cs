using System;
using System.Collections.Generic;
using System.Web.Http;
using LP.Helpers;
using LP.DB;

namespace LP.Pool
{
    public class MinersController : ApiController
    {
        public IHttpActionResult Get()
        {
            List<MinerData> data = Miner.get24HMiners();

            return Json(data.ConvertAll(m => new
            {
                Address = m.Address,
                LastSeen = Utils.processLastSeen(m.LastSeen),
                RoundShares = m.RoundShares,
                Pending = m.Pending.ToString("F"),
                HashRate = (m.HashRate / 1000.0).ToString("F")
            }));
        }
    }
}
