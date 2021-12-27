using System;
using System.Collections.Generic;
using LP.DB;

namespace LP.Pool
{
    public class Miner
    {
        PoolDB.MinerDBType minerDB = null;
        PoolDB.WorkerDBType workerDB = null;
        PoolDB.ShareDBType shareDB = null;

        public Miner(string wallet)
        {
            minerDB = PoolDB.Instance.getMiner(wallet);
            if (minerDB == null)
            {
                minerDB = new PoolDB.MinerDBType
                {
                    id = -1,
                    address = wallet,
                    pending = 0,
                    lastSeen = DateTime.Now
                };
            }
            else
            {
                minerDB.lastSeen = DateTime.Now;
            }
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
            if(minerDB != null)
            {
                workerDB = PoolDB.Instance.getWorker(minerDB.id, worker);
                if(workerDB == null)
                {
                    workerDB = new PoolDB.WorkerDBType
                    {
                        id = -1,
                        minerId = minerDB.id,
                        name = worker,
                        lastSeen = DateTime.Now
                    };
                }
                else
                {
                    workerDB.lastSeen = DateTime.Now;
                }
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
                shareDB = new PoolDB.ShareDBType
                {
                    id = -1,
                    blockNum = (long)blocknum,
                    difficulty = (long)difficulty,
                    minerId = minerDB.id,
                    nonce = nonce,
                    blockResolved = verify_result,
                    processed = false,
                    timeStamp = DateTime.Now,
                    workerId = workerDB.id
                };
            }
        }

        public static void getActiveMinersCount(out int activeMinersCount, out int activeWorkersCount)
        {
            PoolDB.Instance.getActiveMinersCount(out activeMinersCount, out activeWorkersCount);
        }
    }
}
