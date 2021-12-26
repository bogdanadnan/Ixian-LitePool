using System;
using System.Collections.Generic;
using IXICore;
using SQLite;

namespace LP.DB
{
    public class PoolDB
    {
        [Table("Miner")]
        public class MinerDBType
        {
            [PrimaryKey, AutoIncrement]
            public int id { get; set; }

            [Unique]
            public string address { get; set; }

            public DateTime lastSeen { get; set; }

            public decimal pending { get; set; }
        }

        [Table("Worker")]
        public class WorkerDBType
        {
            [PrimaryKey, AutoIncrement]
            public int id { get; set; }

            [Indexed]
            public int minerId { get; set; }

            public string name { get; set; }

            public string miningApp { get; set; }

            public double hashrate { get; set; }

            public DateTime lastSeen { get; set; }
        }

        [Table("Share")]
        public class ShareDBType
        {
            [PrimaryKey, AutoIncrement]
            public int id { get; set; }

            [Indexed]
            public int minerId { get; set; }

            [Indexed]
            public int workerId { get; set; }

            public DateTime timeStamp { get; set; }

            public long blockNum { get; set; }

            public long difficulty { get; set; }

            [Indexed]
            public string nonce { get; set; }

            public bool blockResolved { get; set; }

            [Indexed]
            public bool processed { get; set; }
        }

        [Table("Block")]
        public class BlockDBType
        {
            [PrimaryKey]
            public long blockNum { get; set; }

            public long difficulty { get; set; }

            public int version { get; set; }

            public byte[] checksum { get; set; }

            public DateTime timeStamp { get; set; }
        }

        [Table("PoolBlock")]
        public class PoolBlockDBType
        {
            [PrimaryKey]
            public long blockNum { get; set; }

            public DateTime miningStart { get; set; }

            public DateTime? miningEnd { get; set; }

            public int resolution { get; set; }

            public long poolDifficulty { get; set; }
        }

        [Table("PowData")]
        public class PowDataDBType
        {
            [PrimaryKey, AutoIncrement]
            public int id { get; set; }

            [Indexed]
            public long blockNum { get; set; }

            [Indexed]
            public long solvedBlock { get; set; }

            [Indexed]
            public string solverAddress { get; set; }

            public string txId { get; set; }

            public decimal reward { get; set; }
        }

        [Table("Payment")]
        public class PaymentDBType
        {
            [PrimaryKey, AutoIncrement]
            public int id { get; set; }

            [Indexed]
            public int minerId { get; set; }

            public DateTime timeStamp { get; set; }

            public decimal value { get; set; }

            public decimal fee { get; set; }

            public string txId { get; set; }

            public bool verified { get; set; }
        }

        private static PoolDB instance = null;

        public static PoolDB Instance
        {
            get
            {
                if(instance == null)
                {
                    instance = new PoolDB();
                }

                return instance;
            }
        }

        private SQLiteConnection db = null;

        public PoolDB()
        {
            db = new SQLiteConnection("pool.sqlite", true);
            initTables();
        }

        private void initTables()
        {
            try
            {
                db.CreateTable(typeof(MinerDBType));
                db.CreateTable(typeof(WorkerDBType));
                db.CreateTable(typeof(ShareDBType));
                db.CreateTable(typeof(BlockDBType));
                db.CreateTable(typeof(PoolBlockDBType));
                db.CreateTable(typeof(PowDataDBType));
                db.CreateTable(typeof(PaymentDBType));
            }
            catch(Exception ex)
            {
                Console.WriteLine("Unhandled exception in initTables {0}", ex.Message);
            }
        }

        public MinerDBType getMiner(string address)
        {
            return db.Table<MinerDBType>().FirstOrDefault(m => m.address == address);
        }

        public int updateMiner(MinerDBType miner)
        {
            if (miner.id == -1)
            {
                int recs = db.Insert(miner);
                if (recs == 0)
                {
                    return -1;
                }
                else
                {
                    return (int)SQLite3.LastInsertRowid(db.Handle);
                }
            }
            else
            {
                db.Update(miner);
                return miner.id;
            }
        }

        public WorkerDBType getWorker(int minerId, string workerName)
        {
            return db.Table<WorkerDBType>().FirstOrDefault(m => m.minerId == minerId && m.name == workerName);
        }

        public int updateWorker(WorkerDBType worker)
        {
            if (worker.id == -1)
            {
                int recs = db.Insert(worker);
                if (recs == 0)
                {
                    return -1;
                }
                else
                {
                    return (int)SQLite3.LastInsertRowid(db.Handle);
                }
            }
            else
            {
                db.Update(worker);
                return worker.id;
            }
        }

        public int addShare(ShareDBType share)
        {
            int recs = db.Insert(share);
            if (recs == 0)
            {
                return -1;
            }
            else
            {
                return (int)SQLite3.LastInsertRowid(db.Handle);
            }
        }

        public BlockDBType getBlock(long blkNum)
        {
            return db.Table<BlockDBType>().FirstOrDefault(m => m.blockNum == blkNum);
        }

        public void addBlock(BlockDBType block)
        {
            db.Insert(block);
        }

        public List<PowDataDBType> getPowDataFromBlock(long blkNum)
        {
            return db.Table<PowDataDBType>().Where(pow => pow.blockNum == blkNum).ToList();
        }

        public void addPowDataForBlock(List<PowDataDBType> powData)
        {
            db.InsertAll(powData);
        }

        public void deletePowDataFromBlock(long blkNum, long minedBlkNum)
        {
            db.Table<PowDataDBType>().Delete(pow => pow.blockNum == blkNum && pow.solvedBlock == minedBlkNum);
        }

        public PoolBlockDBType getPoolBlock(long blkNum)
        {
            return db.Table<PoolBlockDBType>().FirstOrDefault(blk => blk.blockNum == blkNum);
        }

        public void updatePoolBlock(PoolBlockDBType blk)
        {
            if(getPoolBlock(blk.blockNum) != null)
            {
                db.Update(blk);
            }
            else
            {
                db.Insert(blk);
            }
        }

        public List<ShareDBType> getUnprocessedShares()
        {
            return db.Table<ShareDBType>().Where(shr => shr.processed == false).ToList();
        }

        public void updateMinerPendingBalance(int minerId, decimal pending)
        {
            var miner = db.Table<MinerDBType>().FirstOrDefault(m => m.id == minerId);
            if(miner != null)
            {
                miner.pending = pending;
                db.Update(miner);
            }
        }

        public List<MinerDBType> getMiners(List<int> minerIds)
        {
            List<MinerDBType> miners = new List<MinerDBType>();
            foreach (var minerId in minerIds)
            {
                var miner = db.Table<MinerDBType>().FirstOrDefault(m => m.id == minerId);
                if(miner != null)
                {
                    miners.Add(miner);
                }
            }
            return miners;
        }

        public void updateShares(List<ShareDBType> shares)
        {
            db.UpdateAll(shares);
        }

        public int addPayment(PaymentDBType payment)
        {
            int recs = db.Insert(payment);
            if (recs == 0)
            {
                return -1;
            }
            else
            {
                return (int)SQLite3.LastInsertRowid(db.Handle);
            }
        }

        public List<PaymentDBType> getUnverifiedPayments()
        {
            var oldestTimeStamp = DateTime.Now - TimeSpan.FromDays(1);
            return db.Table<PaymentDBType>().Where(p => p.verified == false && p.timeStamp > oldestTimeStamp).ToList();
        }

        public void updatePayment(PaymentDBType payment)
        {
            db.Update(payment);
        }

        public bool shareExists(string nonce)
        {
            return db.Table<ShareDBType>().FirstOrDefault(shr => shr.nonce == nonce) != null;
        }
    }
}
