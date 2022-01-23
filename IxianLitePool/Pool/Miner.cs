using System;
using System.Collections.Generic;
using LP.DB;

namespace LP.Pool
{
    public class MinerDashboardInformation
    {
        public int Workers { get; set; }
        public double Hashrate { get; set; }
        public decimal Pending { get; set; }
        public decimal Payments { get; set; }
    };

    public class Miner
    {
        MinerDBType minerDB = null;
        WorkerDBType workerDB = null;
        ShareDBType shareDB = null;

        public Miner(string wallet)
        {
            minerDB = PoolDB.Instance.getMiner(wallet);
            minerDB.lastSeen = DateTime.Now;
        }

        public bool isValid()
        {
            return minerDB != null;
        }

        public void commit()
        {
            minerDB.id = PoolDB.Instance.updateMiner(minerDB);
            if(workerDB != null)
            {
                workerDB.minerId = minerDB.id;
                workerDB.id = PoolDB.Instance.updateWorker(workerDB);
            }
            if(shareDB != null)
            {
                shareDB.minerId = minerDB.id;
                shareDB.workerId = workerDB.id;
                shareDB.id = PoolDB.Instance.addShare(shareDB);
            }
        }

        public void selectWorker(string id, string worker)
        {
            if (minerDB != null)
            {
                workerDB = PoolDB.Instance.getWorker(minerDB.id, worker);
                workerDB.lastSeen = DateTime.Now;
            }
        }

        public void updateWorker(double hr, string version)
        {
            if (workerDB != null)
            {
                workerDB.hashrate = hr;
                workerDB.miningApp = version;
            }
        }

        public void addShare(ulong blocknum, string nonce, ulong difficulty, bool verify_result)
        {
            if (minerDB != null && workerDB != null)
            {
                shareDB = PoolDB.Instance.createShare((long)blocknum, (long)difficulty, nonce, verify_result, minerDB.id, workerDB.id);
            }
        }

        public static void getActiveMinersCount(out int activeMinersCount, out int activeWorkersCount)
        {
            PoolDB.Instance.getActiveMinersCount(out activeMinersCount, out activeWorkersCount);
        }

        public static List<MinerData> get24HMiners()
        {
            return PoolDB.Instance.getMinersDataForLast(24);
        }

        public static MinerDashboardInformation getMinerInformation(string address)
        {
            return new MinerDashboardInformation
            {
                Hashrate = PoolDB.Instance.getMinerHashrate(address),
                Workers = PoolDB.Instance.getMinerWorkersCount(address),
                Payments = PoolDB.Instance.getMinerTotalPayments(address),
                Pending = PoolDB.Instance.getMinerPendingValue(address)
            };
        }

        public static List<MinerWorker> getMinerWorkersInformation(string address)
        {
            return PoolDB.Instance.getMinerWorkersInformation(address);
        }

        public static List<MinerPayment> getMinerPaymentsInformation(string address)
        {
            return PoolDB.Instance.getMinerPaymentsInformation(address);
        }

        public static bool checkAddress(string address)
        {
            var miner = PoolDB.Instance.getMiner(address);
            return miner.id != -1;
        }
    }
}
