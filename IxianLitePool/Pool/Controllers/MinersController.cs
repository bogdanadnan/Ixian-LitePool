using System;
using System.Collections.Generic;
using System.Web.Http;
using static LP.DB.PoolDB;

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
                LastSeen = processLastSeen(m.LastSeen),
                RoundShares = m.RoundShares,
                Pending = m.Pending.ToString("F"),
                HashRate = (m.HashRate / 1000.0).ToString("F")
            }));
        }

        private string processLastSeen(DateTime dt)
        {
            TimeSpan ts = DateTime.Now - dt;

            if (ts.TotalHours >= 1)
            {
                return String.Format("{0} hour{1} ago", Math.Floor(ts.TotalHours), Math.Floor(ts.TotalHours) > 1 ? "s" : "");
            }
            if (ts.TotalMinutes >= 1)
            {
                return String.Format("{0} minute{1} ago", Math.Floor(ts.TotalMinutes), Math.Floor(ts.TotalMinutes) > 1 ? "s" : "");
            }
            if (ts.TotalSeconds >= 1)
            {
                return String.Format("{0} second{1} ago", Math.Floor(ts.TotalSeconds), Math.Floor(ts.TotalSeconds) > 1 ? "s" : "");
            }

            return "1 second ago";
        }
    }
}
