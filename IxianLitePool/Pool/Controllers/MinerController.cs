using System;
using System.Collections.Generic;
using System.Web.Http;
using LP.Helpers;
using static LP.DB.PoolDB;

namespace LP.Pool
{
    public class MinerController : ApiController
    {
        [Route("api/miner/{address}/verify")]
        [HttpGet]
        public IHttpActionResult Verify(string address)
        {
            var valid = Miner.checkAddress(address);
            return Json(new
            {
                Valid = valid
            });
        }

        [Route("api/miner/{address}/dashboard")]
        [HttpGet]
        public IHttpActionResult Dashboard(string address)
        {
            MinerDashboardInformation miner = Miner.getMinerInformation(address);

            return Json(miner);
        }

        [Route("api/miner/{address}/workers")]
        [HttpGet]
        public IHttpActionResult Workers(string address)
        {
            List<MinerWorker> workers = Miner.getMinerWorkersInformation(address); 

            return Json(workers.ConvertAll(w => new
            {
                Name = w.Name,
                Hashrate = (w.Hashrate / 1000.0).ToString("F"),
                Shares = w.Shares,
                LastSeen = Utils.processLastSeen(w.LastSeen)
            }));
        }

        [Route("api/miner/{address}/payments")]
        [HttpGet]
        public IHttpActionResult Payments(string address)
        {
            List<MinerPayment> payments = Miner.getMinerPaymentsInformation(address);

            return Json(payments.ConvertAll(p => new
            {
                TxId = p.TxId,
                TimeStamp = Utils.processDateTime(p.TimeStamp),
                Value = p.Value.ToString("F"),
                Status = p.Status,
            }));
        }
    }
}
