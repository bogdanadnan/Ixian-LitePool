using System;
using System.Collections.Generic;
using System.Linq;
using IXICore;
using IXICore.Meta;
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

            [Indexed]
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

            [Indexed]
            public long blockNum { get; set; }

            public long difficulty { get; set; }

            [Indexed]
            public string nonce { get; set; }

            [Indexed]
            public bool blockResolved { get; set; }

            [Indexed]
            public bool processed { get; set; }

            [Indexed]
            public string paymentSession { get; set; }
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

            public string paymentSession { get; set; }
        }

        [Table("PoolState")]
        public class PoolStateDBType
        {
            [PrimaryKey, AutoIncrement]
            public int id { get; set; }

            [Indexed]
            public string key { get; set; }

            public string value { get; set; }
        }

        [Table("Notification")]
        public class NotificationDBType
        {
            [PrimaryKey, AutoIncrement]
            public int id { get; set; }

            public int type { get; set; }

            public string notification { get; set; }

            [Indexed]
            public bool active { get; set; }
        }

        public class DoubleData
        {
            public double value { get; set; }
        }

        public class IntegerData
        {
            public int value { get; set; }
        }

        public class DecimalData
        {
            public decimal value { get; set; }
        }

        public class MinerData
        {
            public string Address { get; set; }
            public DateTime LastSeen { get; set; }
            public int RoundShares { get; set; }
            public decimal Pending { get; set; }
            public double HashRate { get; set; }
        }

        public class BlockData
        {
            public long BlockNum { get; set; }
            public DateTime TimeStamp { get; set; }
            public decimal Reward { get; set; }
            public string Status { get; set; }
            public string MinerAddress { get; set; }
        }

        public class PaymentData
        {
            public string MinerAddress { get; set; }
            public DateTime TimeStamp { get; set; }
            public decimal Value { get; set; }
            public string Status { get; set; }
            public string TxId { get; set; }
        }

        public class MinerWorker
        {
            public string Name { get; set; }
            public double Hashrate { get; set; }
            public int Shares { get; set; }
            public DateTime LastSeen { get; set; }
        }

        public class MinerPayment
        {
            public string TxId { get; set; }
            public DateTime TimeStamp { get; set; }
            public decimal Value { get; set; }
            public string Status { get; set; }
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
//            db.Tracer = new Action<string>(q => Console.WriteLine(q));
//            db.Trace = true;
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
                db.CreateTable(typeof(PoolStateDBType));
                db.CreateTable(typeof(NotificationDBType));
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
                    return miner.id > 0 ? miner.id : (int)SQLite3.LastInsertRowid(db.Handle);
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
                    return worker.id > 0 ? worker.id : (int)SQLite3.LastInsertRowid(db.Handle);
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
                return share.id > 0 ? share.id : (int)SQLite3.LastInsertRowid(db.Handle);
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

        public List<ShareDBType> getUnprocessedShares(string paymentSession)
        {
            db.Execute("UPDATE Share SET paymentSession = ?, processed = 1 WHERE processed = 0", paymentSession);
            return db.Table<ShareDBType>().Where(shr => shr.paymentSession == paymentSession).ToList();
        }

        public void cleanUpShares()
        {
            var limit = DateTime.Now - TimeSpan.FromDays(1);
            db.Execute("DELETE FROM Share WHERE processed = 1 AND blockResolved = 0 AND timeStamp < ?", limit);
        }

        public void cleanUpBlocks(long blockLimit)
        {
            db.Execute("DELETE FROM Block WHERE blockNum < ?", blockLimit);
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

        public int addPayment(PaymentDBType payment)
        {
            int recs = db.Insert(payment);
            if (recs == 0)
            {
                return -1;
            }
            else
            {
                return payment.id > 0 ? payment.id : (int)SQLite3.LastInsertRowid(db.Handle);
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

        public void getActiveMinersCount(out int activeMinersCount, out int activeWorkersCount)
        {
            var limit = DateTime.Now - (new TimeSpan(0, 5, 0));
            activeMinersCount = db.Table<MinerDBType>().Count(m => m.lastSeen > limit);
            activeWorkersCount = db.Table<WorkerDBType>().Count(m => m.lastSeen > limit);
        }

        public decimal getTotalPayments()
        {
            var result = db.Query<DecimalData>("SELECT SUM(\"value\") AS \"value\" FROM \"Payment\" WHERE \"verified\" = 1").FirstOrDefault();
            return result != null ? result.value : 0;
        }

        public double getTotalHashrate()
        {
            var limit = DateTime.Now - (new TimeSpan(0, 5, 0));
            var result = db.Query<DoubleData>("SELECT SUM(\"hashrate\") AS \"value\" FROM \"Worker\" WHERE \"lastSeen\" > ?", limit).FirstOrDefault();
            return result != null ? result.value : 0;
        }

        public int getBlocksMinedSince(DateTime since)
        {
            var result = db.Query<IntegerData>("SELECT COUNT(\"blockNum\") AS \"value\" FROM \"PoolBlock\" WHERE \"miningStart\" > ? AND \"resolution\" = 2", since).FirstOrDefault();
            return result != null ? result.value : 0;
        }

        public List<MinerData> getMinersDataForLast(int hours)
        {
            var limitMiner = DateTime.Now - (new TimeSpan(hours, 0, 0));
            var limitWorker = DateTime.Now - (new TimeSpan(0, 5, 0));

            return db.Query<MinerData>(@"SELECT Miner.address AS Address, Miner.lastSeen AS LastSeen, Miner.pending AS Pending, 
                (SELECT SUM(Worker.hashrate) FROM Worker WHERE Worker.minerId = Miner.id AND Worker.lastSeen > ?) AS HashRate,
                (SELECT COUNT(Share.id) FROM Share WHERE Share.minerId = Miner.Id AND Share.processed = 0) AS RoundShares
                FROM Miner
                WHERE Miner.lastSeen > ?
                ORDER BY RoundShares DESC, Miner.lastSeen DESC", limitWorker, limitMiner).ToList();
        }

        public List<BlockData> getMinedBlocks()
        {
            var address = new Address(IxianHandler.getWalletStorage().getPrimaryAddress()).ToString();

            return db.Query<BlockData>(@"SELECT DISTINCT PoolBlock.blockNum AS BlockNum, PoolBlock.miningEnd AS TimeStamp, PowData.reward AS Reward,
                    IIF(PowData.id IS NULL, 'Unconfirmed', 'Confirmed') AS Status, Miner.address AS MinerAddress
                FROM PoolBlock
            	    LEFT JOIN PowData ON PowData.solvedBlock = PoolBlock.blockNum AND PowData.solverAddress = ?
	                LEFT JOIN Share ON Share.blockNum = PoolBlock.blockNum AND Share.blockResolved = 1
                        AND Share.timeStamp = (SELECT MIN(Share.timeStamp) FROM Share WHERE Share.blockNum = PoolBlock.blockNum AND Share.blockResolved = 1)
	                LEFT JOIN Miner ON Miner.id = Share.minerId
	            WHERE PoolBlock.resolution = 2
	            ORDER BY PoolBlock.miningEnd DESC", address).ToList();
        }

        public List<PaymentData> getPayments()
        {
            return db.Query<PaymentData>(@"SELECT IIF(Payment.minerId = -1, 'Pool Fee', Miner.address) AS MinerAddress, Payment.timeStamp AS TimeStamp, Payment.value AS Value, 
        	    	Payment.txId AS TxId, IIF(Payment.verified = 1, 'Verified', 'Pending') AS Status
                FROM Payment
		            LEFT JOIN Miner ON Miner.id = Payment.minerId
                ORDER BY Payment.timeStamp DESC").ToList();
        }

        public List<PoolStateDBType> getAllPoolStates()
        {
            return db.Table<PoolStateDBType>().ToList();
        }

        public int setPoolState(string key, string value)
        {
            var entry = db.Table<PoolStateDBType>().Where(ps => ps.key == key).FirstOrDefault();
            if(entry != null)
            {
                entry.value = value;
                db.Update(entry);
                return entry.id;
            }
            else
            {
                entry = new PoolStateDBType
                {
                    id = -1,
                    key = key,
                    value = value
                };

                int recs = db.Insert(entry);

                if (recs == 0)
                {
                    return -1;
                }
                else
                {
                    return entry.id > 0 ? entry.id : (int)SQLite3.LastInsertRowid(db.Handle);
                }
            }
        }

        public double getMinerHashrate(string address)
        {
            var limit = DateTime.Now - (new TimeSpan(0, 5, 0));
            var result = db.Query<DoubleData>(@"SELECT SUM(Worker.hashrate) AS value 
                                                    FROM Worker
                                                        JOIN Miner ON Worker.minerId = Miner.id AND Miner.address = ?
                                                     WHERE Worker.lastSeen > ?", address, limit).FirstOrDefault();
            return result != null ? result.value : 0;
        }

        public int getMinerWorkersCount(string address)
        {
            var limit = DateTime.Now - (new TimeSpan(0, 5, 0));
            var result = db.Query<IntegerData>(@"SELECT COUNT(Worker.id) AS value 
                                                    FROM Worker
                                                        JOIN Miner ON Worker.minerId = Miner.id AND Miner.address = ?
                                                     WHERE Worker.lastSeen > ?", address, limit).FirstOrDefault();
            return result != null ? result.value : 0;
        }

        public decimal getMinerTotalPayments(string address)
        {
            var result = db.Query<DecimalData>(@"SELECT SUM(Payment.value) AS value 
                                                    FROM Payment
                                                        JOIN Miner ON Payment.minerId = Miner.id AND Miner.address = ?", address).FirstOrDefault();
            return result != null ? result.value : 0;
        }

        public decimal getMinerPendingValue(string address)
        {
            var miner = db.Table<MinerDBType>().Where(m => m.address == address).FirstOrDefault();
            return miner != null ? miner.pending : 0;
        }

        public List<MinerWorker> getMinerWorkersInformation(string address)
        {
            var limit = DateTime.Now - (new TimeSpan(0, 5, 0));

            return db.Query<MinerWorker>(@"SELECT Worker.name AS Name, Worker.hashrate AS Hashrate,
                (SELECT COUNT(Share.id) FROM Share WHERE Share.workerId == Worker.id AND Share.processed = 0) AS Shares, Worker.lastSeen AS LastSeen
                    FROM Worker
                        JOIN Miner ON Miner.id = Worker.minerId AND Miner.address = ?
                 WHERE Worker.lastSeen > ? ORDER BY Worker.lastSeen DESC", address, limit).ToList();
        }

        public List<MinerPayment> getMinerPaymentsInformation(string address)
        {
            return db.Query<MinerPayment>(@"SELECT Payment.txId AS TxId, Payment.timeStamp AS TimeStamp, Payment.value AS Value, IIF(Payment.verified = 1, 'Verified', 'Pending') AS Status
                                            FROM Payment
                                                JOIN Miner ON Payment.minerId = Miner.id AND Miner.address = ?
                                            ORDER BY Payment.timeStamp DESC", address).ToList();
        }

        public List<MinerDBType> getMinersWithPendingBalance()
        {
            return db.Table<MinerDBType>().Where(m => m.pending > 0).ToList();
        }

        public int addNotification(NotificationDBType entry)
        {
            int recs = db.Insert(entry);

            if (recs == 0)
            {
                return -1;
            }
            else
            {
                return entry.id > 0 ? entry.id : (int)SQLite3.LastInsertRowid(db.Handle);
            }
        }

        public void updateNotificationStatus(int id, bool status)
        {
            var notification = db.Table<NotificationDBType>().Where(n => n.id == id).FirstOrDefault();
            if(notification != null)
            {
                notification.active = status;
                db.Update(notification);
            }
        }

        public List<NotificationDBType> getActiveNotifications()
        {
            return db.Table<NotificationDBType>().Where(n => n.active == true).OrderBy(n => n.id).ToList();
        }
    }
}
