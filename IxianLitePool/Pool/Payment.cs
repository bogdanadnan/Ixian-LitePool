using System;
using System.Collections.Generic;
using System.Threading;
using LP.Meta;
using LP.DB;
using static LP.DB.PoolDB;
using System.Linq;
using IXICore;

namespace LP.Pool
{
    public class Payment
    {
        private static Payment payment = null;
        public static Payment Instance
        {
            get
            {
                if(payment == null)
                {
                    payment = new Payment();
                }
                return payment;
            }
        }

        private Thread paymentProcessorThread = null;
        private bool paymentProcessorRunning = false;
        private Node node = null;
        private DateTime lastPaymentTimeStamp;

        public Payment()
        {}

        public void start(Node node)
        {
            this.node = node;
            if (paymentProcessorThread == null && !paymentProcessorRunning)
            {
                paymentProcessorRunning = true;
                lastPaymentTimeStamp = DateTime.Now;
                paymentProcessorThread = new Thread(paymentProcessor);
            }
        }

        public void stop()
        {
            if (paymentProcessorThread != null && paymentProcessorRunning)
            {
                paymentProcessorRunning = false;
                paymentProcessorThread.Join();
            }
        }

        private void paymentProcessor()
        {
            while(paymentProcessorRunning)
            {
                Thread.Sleep(1000);

                if(DateTime.Now.Minute == 0 && (DateTime.Now - lastPaymentTimeStamp).TotalMinutes > 2)
                {
                    processPayments();
                    lastPaymentTimeStamp = DateTime.Now;
                }
            }
        }

        public void updatePendingPayments()
        {
            List<ShareDBType> shares = PoolDB.Instance.getUnprocessedShares();
            List<MinerDBType> miners = PoolDB.Instance.getMiners(shares.Select(shr => shr.minerId).Distinct().ToList());
            Dictionary<int, List<ShareDBType>> sharesByMiner = shares.Where(shr => miners.Any(m => m.id == shr.minerId))
                .GroupBy(sh => sh.minerId).ToDictionary(k => k.Key, v => v.ToList());
            int shareCount = 0;
            sharesByMiner.Values.ToList().ForEach(shrList => shareCount += shrList.Count);

            var balance = ((decimal)node.getBalance().balance.getAmount()) / 100000000;
            if(balance > 100) // safety net to avoid running this too many times if some balance remains in the wallet
            {
                foreach (var miner in sharesByMiner)
                {
                    decimal pendingValue = balance * miner.Value.Count / shareCount;
                    PoolDB.Instance.updateMinerPendingBalance(miner.Key, pendingValue);
                }
            }
        }

        public void processPayments()
        {
            List<ShareDBType> shares = PoolDB.Instance.getUnprocessedShares();
            List<MinerDBType> miners = PoolDB.Instance.getMiners(shares.Select(shr => shr.minerId).Distinct().ToList());
            Dictionary<int, List<ShareDBType>> sharesByMiner = shares.Where(shr => miners.Any(m => m.id == shr.minerId))
                .GroupBy(sh => sh.minerId).ToDictionary(k => k.Key, v => v.ToList());
            int shareCount = 0;
            sharesByMiner.Values.ToList().ForEach(shrList => shareCount += shrList.Count);

            var balance = node.getBalance().balance;

            if (balance > 100)
            {
                foreach (var minerShare in sharesByMiner)
                {
                    IxiNumber pendingValue = (balance * minerShare.Value.Count / shareCount) - ConsensusConfig.transactionPrice;
                    var miner = miners.FirstOrDefault(m => m.id == minerShare.Key);
                    if(miner != null)
                    {
                        string txId = node.sendTransaction(miner.address, pendingValue);
                        if(!String.IsNullOrEmpty(txId))
                        {
                            PaymentDBType payment = new PaymentDBType
                            {
                                id = -1,
                                minerId = miner.id,
                                txId = txId,
                                value = ((decimal)pendingValue.getAmount()) / 100000000,
                                fee = ((decimal)ConsensusConfig.transactionPrice.getAmount()) / 100000000,
                                timeStamp = DateTime.Now,
                                verified = false
                            };

                            PoolDB.Instance.addPayment(payment);
                        }
                        minerShare.Value.ForEach(shr => shr.processed = true);
                        PoolDB.Instance.updateShares(minerShare.Value);
                        PoolDB.Instance.updateMinerPendingBalance(miner.id, 0);
                    }
                }
            }
        }
    }
}
