using System;
using System.Collections.Generic;
using System.Web.Http;
using LP.Helpers;
using static LP.DB.PoolDB;

namespace LP.Pool
{
    public class PaymentsController : ApiController
    {
        public IHttpActionResult Get()
        {
            List<PaymentData> data = Payment.getPayments();

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
