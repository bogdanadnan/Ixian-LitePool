using System;
using System.Collections.Generic;
using System.Web.Http;
using LP.Helpers;
using Microsoft.Extensions.Caching.Memory;
using static LP.DB.PoolDB;

namespace LP.Pool
{
    public class PaymentsController : ApiController
    {
        public IHttpActionResult Get()
        {
            List<PaymentData> data = null;

            if (MemCache.Instance.TryGetValue("payments_data", out data) && data != null)
            {
                return Json(data.ConvertAll(m => new
                {
                    Miner = m.MinerAddress,
                    TimeStamp = Utils.processDateTime(m.TimeStamp),
                    Value = m.Value.ToString("F"),
                    Status = m.Status,
                    TxId = m.TxId
                }));
            }

            data = Payment.getPayments();

            MemCache.Instance.Set("payments_data", data, new TimeSpan(0, 1, 0));

            return Json(data.ConvertAll(m => new
            {
                Miner = m.MinerAddress,
                TimeStamp = Utils.processDateTime(m.TimeStamp),
                Value = m.Value.ToString("F"),
                Status = m.Status,
                TxId = m.TxId
            }));
        }
    }
}
