using System;
using System.Collections.Generic;
using System.Linq;
using IXICore;
using IXICore.Meta;
using LP.Meta;
using SQLite;

namespace LP.DB
{
    [Table("Miner")]
    public class MinerDBSQLite : MinerDBType
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
    public class WorkerDBSQLite : WorkerDBType
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
    public class ShareDBSQLite : ShareDBType
    {
        [PrimaryKey, AutoIncrement]
        public int id { get; set; }

        [Indexed]
        public int minerId { get; set; }

        [Indexed]
        public int workerId { get; set; }

        [Indexed]
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
    public class BlockDBSQLite : BlockDBType
    {
        [PrimaryKey]
        public long blockNum { get; set; }

        public long difficulty { get; set; }

        public int version { get; set; }

        public byte[] checksum { get; set; }

        public DateTime timeStamp { get; set; }
    }

    [Table("PoolBlock")]
    public class PoolBlockDBSQLite : PoolBlockDBType
    {
        [PrimaryKey]
        public long blockNum { get; set; }

        public DateTime miningStart { get; set; }

        public DateTime? miningEnd { get; set; }

        public int resolution { get; set; }

        public long poolDifficulty { get; set; }
    }

    [Table("PowData")]
    public class PowDataDBSQLite : PowDataDBType
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
    public class PaymentDBSQLite : PaymentDBType
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
    public class PoolStateDBSQLite : PoolStateDBType
    {
        [PrimaryKey, AutoIncrement]
        public int id { get; set; }

        [Indexed]
        public string key { get; set; }

        public string value { get; set; }
    }

    [Table("Notification")]
    public class NotificationDBSQLite : NotificationDBType
    {
        [PrimaryKey, AutoIncrement]
        public int id { get; set; }

        public int type { get; set; }

        public string notification { get; set; }

        [Indexed]
        public bool active { get; set; }
    }

    public class PoolSQLiteDB : IPoolDB
    {
        private SQLiteConnection db = null;

        public PoolSQLiteDB()
        {
            db = new SQLiteConnection(Config.dbConnectionString, true);
            //            db.Tracer = new Action<string>(q => Console.WriteLine(q));
            //            db.Trace = true;
            initTables();
        }

        private void initTables()
        {
            try
            {
                db.CreateTable(typeof(MinerDBSQLite));
                db.CreateTable(typeof(WorkerDBSQLite));
                db.CreateTable(typeof(ShareDBSQLite));
                db.CreateTable(typeof(BlockDBSQLite));
                db.CreateTable(typeof(PoolBlockDBSQLite));
                db.CreateTable(typeof(PowDataDBSQLite));
                db.CreateTable(typeof(PaymentDBSQLite));
                db.CreateTable(typeof(PoolStateDBSQLite));
                db.CreateTable(typeof(NotificationDBSQLite));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unhandled exception in initTables {0}", ex.Message);
            }
        }

        public MinerDBType getMiner(string address)
        {
            var miner = db.Table<MinerDBSQLite>().FirstOrDefault(m => m.address == address);
            if(miner == null)
            {
                miner = new MinerDBSQLite
                {
                    id = -1,
                    address = address,
                    pending = 0,
                    lastSeen = DateTime.Now
                };
            }
            return miner;
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
            var worker = db.Table<WorkerDBSQLite>().FirstOrDefault(m => m.minerId == minerId && m.name == workerName);
            if(worker == null)
            {
                worker = new WorkerDBSQLite
                {
                    id = -1,
                    minerId = minerId,
                    name = workerName,
                    lastSeen = DateTime.Now
                };
            }
            return worker;
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

        public ShareDBType createShare(long blocknum, long difficulty, string nonce, bool blockResolved, int minerId, int workerId)
        {
            return new ShareDBSQLite
            {
                id = -1,
                blockNum = blocknum,
                difficulty = difficulty,
                nonce = nonce,
                blockResolved = blockResolved,
                minerId = minerId,
                workerId = workerId,
                paymentSession = null,
                processed = false,
                timeStamp = DateTime.Now
            };
        }

        public BlockDBType getBlock(long blkNum)
        {
            return db.Table<BlockDBSQLite>().FirstOrDefault(m => m.blockNum == blkNum);
        }

        public void addBlock(long blockNum, int version, long difficulty, DateTime timeStamp, byte[] blockChecksum)
        {
            db.Insert(new BlockDBSQLite
            {
                blockNum = blockNum,
                version = version,
                difficulty = difficulty,
                timeStamp = timeStamp,
                checksum = blockChecksum
            });
        }

        public List<PowDataDBType> getPowDataFromBlock(long blkNum)
        {
            return db.Table<PowDataDBSQLite>().Where(pow => pow.blockNum == blkNum).ToList().ConvertAll(x => (PowDataDBType)x);
        }

        public void addPowDataForBlock(long blockNum, decimal reward, long solvedBlock, string solverAddress, string txId)
        {
            db.Insert(new PowDataDBSQLite
            {
                blockNum = blockNum,
                reward = reward,
                solvedBlock = solvedBlock,
                solverAddress = solverAddress,
                txId = txId
            });
        }

        public void deletePowDataFromBlock(long blkNum, long minedBlkNum)
        {
            db.Table<PowDataDBSQLite>().Delete(pow => pow.blockNum == blkNum && pow.solvedBlock == minedBlkNum);
        }

        public PoolBlockDBType getPoolBlock(long blkNum)
        {
            return db.Table<PoolBlockDBSQLite>().FirstOrDefault(blk => blk.blockNum == blkNum);
        }

        public void addPoolBlock(long blockNum, long difficulty)
        {
            var blk = getPoolBlock(blockNum);
            if (blk != null)
            {
                blk.resolution = 0;
                blk.miningStart = DateTime.Now;
                blk.poolDifficulty = difficulty;
                db.Update(blk);
            }
            else
            {
                blk = new PoolBlockDBSQLite
                {
                    blockNum = blockNum,
                    miningStart = DateTime.Now,
                    miningEnd = null,
                    poolDifficulty = difficulty,
                    resolution = 0
                };

                db.Insert(blk);
            }
        }

        public void updatePoolBlock(long blockNum, int blkResolution, DateTime miningEnd)
        {
            var blk = getPoolBlock(blockNum);
            if (blk != null)
            {
                blk.resolution = blkResolution;
                blk.miningEnd = miningEnd;
                db.Update(blk);
            }
        }

        public List<ShareDBType> getUnprocessedShares()
        {
            return db.Table<ShareDBSQLite>().Where(shr => shr.processed == false).ToList().ConvertAll(x => (ShareDBType)x);
        }

        public List<ShareDBType> getUnprocessedShares(string paymentSession)
        {
            db.Execute("UPDATE Share SET paymentSession = ?, processed = 1 WHERE processed = 0", paymentSession);
            return db.Table<ShareDBSQLite>().Where(shr => shr.paymentSession == paymentSession).ToList().ConvertAll(x => (ShareDBType)x);
        }

        public void cleanUpShares()
        {
            db.Execute("DELETE FROM Share WHERE processed = 1 AND blockResolved = 0");
        }

        public void cleanUpBlocks(long blockLimit)
        {
            db.Execute("DELETE FROM Block WHERE blockNum < ?", blockLimit);
        }

        public void updateMinerPendingBalance(int minerId, decimal pending)
        {
            var miner = db.Table<MinerDBSQLite>().FirstOrDefault(m => m.id == minerId);
            if (miner != null)
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
                var miner = db.Table<MinerDBSQLite>().FirstOrDefault(m => m.id == minerId);
                if (miner != null)
                {
                    miners.Add(miner);
                }
            }
            return miners;
        }

        public PaymentDBType addPayment(int minerId, string txId, decimal value, decimal fee, DateTime timeStamp, string paymentSession)
        {
            var payment = new PaymentDBSQLite
            {
                id = -1,
                minerId = minerId,
                txId = txId,
                value = value,
                fee = fee,
                timeStamp = timeStamp,
                verified = false,
                paymentSession = paymentSession
            };

            int recs = db.Insert(payment);
            if (recs == 0)
            {
                return null;
            }
            else
            {
                if (payment.id < 0)
                {
                    payment.id = (int)SQLite3.LastInsertRowid(db.Handle);
                }
                return payment;
            }
        }

        public List<PaymentDBType> getUnverifiedPayments()
        {
            var oldestTimeStamp = DateTime.Now - TimeSpan.FromDays(1);
            return db.Table<PaymentDBSQLite>().Where(p => p.verified == false && p.timeStamp > oldestTimeStamp).ToList().ConvertAll(x => (PaymentDBType)x);
        }

        public void setPaymentVerified(int paymentId)
        {
            db.Execute("UPDATE Payment SET verified = 1 WHERE id = ?", paymentId);
        }

        public bool shareExists(string nonce)
        {
            return db.Table<ShareDBSQLite>().FirstOrDefault(shr => shr.nonce == nonce) != null;
        }

        public void getActiveMinersCount(out int activeMinersCount, out int activeWorkersCount)
        {
            var limit = DateTime.Now - (new TimeSpan(0, 5, 0));
            activeMinersCount = db.Table<MinerDBSQLite>().Count(m => m.lastSeen > limit);
            activeWorkersCount = db.Table<WorkerDBSQLite>().Count(m => m.lastSeen > limit);
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
	            ORDER BY PoolBlock.miningEnd DESC
                LIMIT 1000", address).ToList();
        }

        public List<PaymentData> getPayments()
        {
            return db.Query<PaymentData>(@"SELECT IIF(Payment.minerId = -1, 'Pool Fee', Miner.address) AS MinerAddress, Payment.timeStamp AS TimeStamp, Payment.value AS Value, 
        	    	Payment.txId AS TxId, IIF(Payment.verified = 1, 'Verified', 'Pending') AS Status
                FROM Payment
		            LEFT JOIN Miner ON Miner.id = Payment.minerId
                ORDER BY Payment.timeStamp DESC
                LIMIT 1000").ToList();
        }

        public List<PoolStateDBType> getAllPoolStates()
        {
            return db.Table<PoolStateDBSQLite>().ToList().ConvertAll(x => (PoolStateDBType)x);
        }

        public PoolStateDBType setPoolState(string key, string value)
        {
            var entry = db.Table<PoolStateDBSQLite>().Where(ps => ps.key == key).FirstOrDefault();
            if (entry != null)
            {
                entry.value = value;
                db.Update(entry);
                return entry;
            }
            else
            {
                entry = new PoolStateDBSQLite
                {
                    id = -1,
                    key = key,
                    value = value
                };

                int recs = db.Insert(entry);

                if (recs == 0)
                {
                    return null;
                }
                else
                {
                    if(entry.id < -1)
                    {
                        entry.id = (int)SQLite3.LastInsertRowid(db.Handle);
                    }
                    return entry;
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
            var miner = db.Table<MinerDBSQLite>().Where(m => m.address == address).FirstOrDefault();
            return miner != null ? miner.pending : 0;
        }

        public List<MinerWorker> getMinerWorkersInformation(string address)
        {
            var limit = DateTime.Now - (new TimeSpan(0, 5, 0));

            return db.Query<MinerWorker>(@"SELECT Worker.name AS Name, Worker.hashrate AS Hashrate,
                (SELECT COUNT(Share.id) FROM Share WHERE Share.workerId = Worker.id AND Share.processed = 0) AS Shares, Worker.lastSeen AS LastSeen
                    FROM Worker
                        JOIN Miner ON Miner.id = Worker.minerId AND Miner.address = ?
                 WHERE Worker.lastSeen > ? ORDER BY Worker.lastSeen DESC", address, limit).ToList();
        }

        public List<MinerPayment> getMinerPaymentsInformation(string address)
        {
            return db.Query<MinerPayment>(@"SELECT Payment.txId AS TxId, Payment.timeStamp AS TimeStamp, Payment.value AS Value, IIF(Payment.verified = 1, 'Verified', 'Pending') AS Status
                                            FROM Payment
                                                JOIN Miner ON Payment.minerId = Miner.id AND Miner.address = ?
                                            ORDER BY Payment.timeStamp DESC
                                            LIMIT 1000", address).ToList();
        }

        public List<MinerDBType> getMinersWithPendingBalance()
        {
            return db.Table<MinerDBSQLite>().Where(m => m.pending > 0).ToList().ConvertAll(x => (MinerDBType)x);
        }

        public int addNotification(int type, string notification, bool active)
        {
            var entry = new NotificationDBSQLite
            {
                type = type,
                notification = notification,
                active = active
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

        public void updateNotificationStatus(int id, bool status)
        {
            var notification = db.Table<NotificationDBSQLite>().Where(n => n.id == id).FirstOrDefault();
            if (notification != null)
            {
                notification.active = status;
                db.Update(notification);
            }
        }

        public List<NotificationDBType> getActiveNotifications()
        {
            return db.Table<NotificationDBSQLite>().Where(n => n.active == true).OrderBy(n => n.id).ToList().ConvertAll(x => (NotificationDBType)x);
        }
    }
}
