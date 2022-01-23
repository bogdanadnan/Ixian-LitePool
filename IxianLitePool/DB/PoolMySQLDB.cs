using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using Dapper.Contrib.Extensions;
using IXICore;
using IXICore.Meta;
using LP.Meta;
using MySqlConnector;

namespace LP.DB
{
    [Table("Miner")]
    public class MinerDBMySQL : MinerDBType
    {
        public int id { get; set; }
        public string address { get; set; }
        public DateTime lastSeen { get; set; }
        public decimal pending { get; set; }
    }

    [Table("Worker")]
    public class WorkerDBMySQL : WorkerDBType
    {
        public int id { get; set; }
        public int minerId { get; set; }
        public string name { get; set; }
        public string miningApp { get; set; }
        public double hashrate { get; set; }
        public DateTime lastSeen { get; set; }
    }

    [Table("Share")]
    public class ShareDBMySQL : ShareDBType
    {
        public int id { get; set; }
        public int minerId { get; set; }
        public int workerId { get; set; }
        public DateTime timeStamp { get; set; }
        public long blockNum { get; set; }
        public long difficulty { get; set; }
        public string nonce { get; set; }
        public bool blockResolved { get; set; }
        public bool processed { get; set; }
        public string paymentSession { get; set; }
    }

    [Table("Block")]
    public class BlockDBMySQL : BlockDBType
    {
        public long blockNum { get; set; }
        public long difficulty { get; set; }
        public int version { get; set; }
        public byte[] checksum { get; set; }
        public DateTime timeStamp { get; set; }
    }

    [Table("PoolBlock")]
    public class PoolBlockDBMySQL : PoolBlockDBType
    {
        public long blockNum { get; set; }
        public DateTime miningStart { get; set; }
        public DateTime? miningEnd { get; set; }
        public int resolution { get; set; }
        public long poolDifficulty { get; set; }
    }

    [Table("PowData")]
    public class PowDataDBMySQL : PowDataDBType
    {
        public int id { get; set; }
        public long blockNum { get; set; }
        public long solvedBlock { get; set; }
        public string solverAddress { get; set; }
        public string txId { get; set; }
        public decimal reward { get; set; }
    }

    [Table("Payment")]
    public class PaymentDBMySQL : PaymentDBType
    {
        public int id { get; set; }
        public int minerId { get; set; }
        public DateTime timeStamp { get; set; }
        public decimal value { get; set; }
        public decimal fee { get; set; }
        public string txId { get; set; }
        public bool verified { get; set; }
        public string paymentSession { get; set; }
    }

    [Table("PoolState")]
    public class PoolStateDBMySQL : PoolStateDBType
    {
        public int id { get; set; }
        public string key { get; set; }
        public string value { get; set; }
    }

    [Table("Notification")]
    public class NotificationDBMySQL : NotificationDBType
    {
        public int id { get; set; }
        public int type { get; set; }
        public string notification { get; set; }
        public bool active { get; set; }
    }

    public class PoolMySQLDB : IPoolDB
    {
        public PoolMySQLDB()
        {
            using (var db = new MySqlConnection(Config.dbConnectionString))
            {
                IEnumerable<string> tables = db.Query<string>("SELECT table_name FROM information_schema.tables;");

                if(!checkTables(tables))
                {
                    throw new Exception("Invalid database.");
                }
            }
        }

        private bool checkTables(IEnumerable<string> tables)
        {
            if (!tables.Any(t => t == "Miner")) return false;
            if (!tables.Any(t => t == "Worker")) return false;
            if (!tables.Any(t => t == "Share")) return false;
            if (!tables.Any(t => t == "Block")) return false;
            if (!tables.Any(t => t == "PoolBlock")) return false;
            if (!tables.Any(t => t == "PowData")) return false;
            if (!tables.Any(t => t == "Payment")) return false;
            if (!tables.Any(t => t == "PoolState")) return false;
            if (!tables.Any(t => t == "Notification")) return false;
            return true;
        }

        public MinerDBType getMiner(string address)
        {
            using (var db = new MySqlConnection(Config.dbConnectionString))
            {
                var miner = db.QuerySingleOrDefault<MinerDBMySQL>("SELECT * FROM Miner WHERE address = @address", new { address = address });
                if (miner == null)
                {
                    miner = new MinerDBMySQL
                    {
                        id = -1,
                        address = address,
                        pending = 0,
                        lastSeen = DateTime.Now
                    };
                }
                return miner;
            }
        }

        public int updateMiner(MinerDBType miner)
        {
            using (var db = new MySqlConnection(Config.dbConnectionString))
            {
                if (miner.id == -1)
                {
                    var recs = db.QueryMultiple("INSERT INTO Miner(address, lastSeen, pending) VALUES(@address, @lastSeen, @pending); SELECT LAST_INSERT_ID();", miner);
                    var id = recs.Read<int>();
                    if (id.Count() == 0)
                    {
                        return -1;
                    }
                    else
                    {
                        return id.First();
                    }
                }
                else
                {
                    db.Execute("UPDATE Miner SET address = @address, lastSeen = @lastSeen, pending = @pending WHERE id = @id", miner);
                    return miner.id;
                }
            }
        }

        public WorkerDBType getWorker(int minerId, string workerName)
        {
            using (var db = new MySqlConnection(Config.dbConnectionString))
            {
                var worker = db.QuerySingleOrDefault<WorkerDBMySQL>("SELECT * FROM Worker WHERE minerId = @minerId AND name = @name", new { minerId = minerId, name = workerName });
                if (worker == null)
                {
                    worker = new WorkerDBMySQL
                    {
                        id = -1,
                        minerId = minerId,
                        name = workerName,
                        lastSeen = DateTime.Now
                    };
                }
                return worker;
            }
        }

        public int updateWorker(WorkerDBType worker)
        {
            using (var db = new MySqlConnection(Config.dbConnectionString))
            {
                if (worker.id == -1)
                {
                    var recs = db.QueryMultiple("INSERT INTO Worker(minerId, name, miningApp, hashrate, lastSeen) VALUES(@minerId, @name, @miningApp, @hashrate, @lastSeen); SELECT LAST_INSERT_ID();", worker);
                    var id = recs.Read<int>();
                    if (id.Count() == 0)
                    {
                        return -1;
                    }
                    else
                    {
                        return id.First();
                    }
                }
                else
                {
                    db.Execute("UPDATE Worker SET minerId = @minerId, name = @name, miningApp = @miningApp, hashrate = @hashrate, lastSeen = @lastSeen WHERE id = @id", worker);
                    return worker.id;
                }
            }
        }

        public int addShare(ShareDBType share)
        {
            using (var db = new MySqlConnection(Config.dbConnectionString))
            {
                var recs = db.QueryMultiple(@"INSERT INTO Share(minerId, workerId, `timeStamp`, blockNum, difficulty, nonce, blockResolved)
                                              VALUES(@minerId, @workerId, @timeStamp, @blockNum, @difficulty, @nonce, @blockResolved);
                                              SELECT LAST_INSERT_ID();", share);
                var id = recs.Read<int>();
                if (id.Count() == 0)
                {
                    return -1;
                }
                else
                {
                    return id.First();
                }
            }
        }

        public ShareDBType createShare(long blocknum, long difficulty, string nonce, bool blockResolved, int minerId, int workerId)
        {
            return new ShareDBMySQL
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
            using (var db = new MySqlConnection(Config.dbConnectionString))
            {
                return db.QuerySingleOrDefault<BlockDBMySQL>("SELECT * FROM Block WHERE blockNum = @blockNum", new { blockNum = blkNum });
            }
        }

        public void addBlock(long blockNum, int version, long difficulty, DateTime timeStamp, byte[] blockChecksum)
        {
            using (var db = new MySqlConnection(Config.dbConnectionString))
            {
                db.Execute(@"INSERT INTO Block(blockNum, difficulty, `version`, checksum, `timeStamp`)
                        VALUES(@blockNum, @difficulty, @version, @checksum, @timeStamp)
                        ON DUPLICATE KEY UPDATE difficulty = @difficulty, `version` = @version,
                            checksum = @checksum, `timeStamp` = @timeStamp", new BlockDBMySQL
                {
                    blockNum = blockNum,
                    version = version,
                    difficulty = difficulty,
                    timeStamp = timeStamp,
                    checksum = blockChecksum
                });
            }
        }

        public List<PowDataDBType> getPowDataFromBlock(long blkNum)
        {
            using (var db = new MySqlConnection(Config.dbConnectionString))
            {
                return db.Query<PowDataDBMySQL>("SELECT * FROM PowData WHERE blockNum = @blockNum", new { blockNum = blkNum }).ToList().ConvertAll(x => (PowDataDBType)x);
            }
        }

        public void addPowDataForBlock(long blockNum, decimal reward, long solvedBlock, string solverAddress, string txId)
        {
            using (var db = new MySqlConnection(Config.dbConnectionString))
            {
                db.Execute(@"INSERT INTO PowData(blockNum, reward, solvedBlock, solverAddress, txId)
                        VALUES(@blockNum, @reward, @solvedBlock, @solverAddress, @txId)", new PowDataDBMySQL
                {
                    blockNum = blockNum,
                    reward = reward,
                    solvedBlock = solvedBlock,
                    solverAddress = solverAddress,
                    txId = txId
                });
            }
        }

        public void deletePowDataFromBlock(long blkNum, long minedBlkNum)
        {
            using (var db = new MySqlConnection(Config.dbConnectionString))
            {
                db.Execute("DELETE FROM PowData WHERE blockNum = @blockNum AND solvedBlock = @solvedBlock", new { blockNum = blkNum, solvedBlock = minedBlkNum });
            }
        }

        public PoolBlockDBType getPoolBlock(long blkNum)
        {
            using (var db = new MySqlConnection(Config.dbConnectionString))
            {
                return db.QuerySingleOrDefault<PoolBlockDBMySQL>("SELECT * FROM PoolBlock WHERE blockNum = @blockNum", new { blockNum = blkNum });
            }
        }

        public void addPoolBlock(long blockNum, long difficulty)
        {
            var blk = getPoolBlock(blockNum);

            if (blk != null)
            {
                using (var db = new MySqlConnection(Config.dbConnectionString))
                {
                    db.Execute("UPDATE PoolBlock SET poolDifficulty = @poolDifficulty, resolution = @resolution, miningStart = @miningStart, miningEnd = NULL WHERE blockNum = @blockNum",
                        new { blockNum = blockNum, poolDifficulty = difficulty, resolution = 0, miningStart = DateTime.Now });
                }
            }
            else
            {
                using (var db = new MySqlConnection(Config.dbConnectionString))
                {
                    db.Execute("INSERT INTO PoolBlock(blockNum, miningStart, miningEnd, poolDifficulty, resolution) VALUES(@blockNum, @miningStart, NULL, @poolDifficulty, 0)", new PoolBlockDBMySQL
                    {
                        blockNum = blockNum,
                        miningStart = DateTime.Now,
                        poolDifficulty = difficulty,
                    });
                }
            }
        }

        public void updatePoolBlock(long blockNum, int blkResolution, DateTime miningEnd)
        {
            var blk = getPoolBlock(blockNum);
            if (blk != null)
            {
                using (var db = new MySqlConnection(Config.dbConnectionString))
                {
                    db.Execute("UPDATE PoolBlock SET resolution = @resolution, miningEnd = @miningEnd WHERE blockNum = @blockNum",
                        new { blockNum = blockNum, resolution = blkResolution, miningEnd = miningEnd });
                }
            }
        }

        public List<ShareDBType> getUnprocessedShares()
        {
            using (var db = new MySqlConnection(Config.dbConnectionString))
            {
                return db.Query<ShareDBMySQL>("SELECT * FROM Share WHERE processed = 0").ToList().ConvertAll(x => (ShareDBType)x);
            }
        }

        public List<ShareDBType> getUnprocessedShares(string paymentSession)
        {
            using (var db = new MySqlConnection(Config.dbConnectionString))
            {
                var recs = db.QueryMultiple(@"UPDATE Share SET paymentSession = @paymentSession, processed = 1 WHERE processed = 0;
                                              SELECT * FROM Share WHERE paymentSession = @paymentSession;",
                                              new { paymentSession = paymentSession });
                return recs.Read<ShareDBMySQL>().ToList().ConvertAll(x => (ShareDBType)x);
            }
        }

        public void cleanUpShares()
        {
            using (var db = new MySqlConnection(Config.dbConnectionString))
            {
                db.Execute("DELETE FROM Share WHERE processed = 1 AND blockResolved = 0");
            }
        }

        public void cleanUpBlocks(long blockLimit)
        {
            using (var db = new MySqlConnection(Config.dbConnectionString))
            {
                db.Execute("DELETE FROM Block WHERE blockNum < @blockNum", new { blockNum = blockLimit });
            }
        }

        public void updateMinerPendingBalance(int minerId, decimal pending)
        {
            using (var db = new MySqlConnection(Config.dbConnectionString))
            {
                db.Execute("UPDATE Miner SET pending = @pending WHERE id = @minerId", new { minerId = minerId, pending = pending });
            }
        }

        public List<MinerDBType> getMiners(List<int> minerIds)
        {
            if(minerIds.Count == 0)
            {
                return new List<MinerDBType>();
            }

            using (var db = new MySqlConnection(Config.dbConnectionString))
            {
                return db.Query<MinerDBMySQL>(String.Format("SELECT * FROM Miner WHERE id IN ({0})", String.Join(",", minerIds.ToArray()))).ToList().ConvertAll(x => (MinerDBType)x);
            }
        }

        public PaymentDBType addPayment(int minerId, string txId, decimal value, decimal fee, DateTime timeStamp, string paymentSession)
        {
            using (var db = new MySqlConnection(Config.dbConnectionString))
            {
                var recs = db.QueryMultiple(@"INSERT INTO Payment(minerId, txId, value, fee, `timeStamp`, verified, paymentSession)
                                              VALUES(@minerId, @txId, @value, @fee, @timeStamp, 0, @paymentSession);
                                              SELECT LAST_INSERT_ID();", new
                {
                    minerId = minerId,
                    txId = txId,
                    value = value,
                    fee = fee,
                    timeStamp = timeStamp,
                    paymentSession = paymentSession
                });
                var id = recs.Read<int>();
                if (id.Count() == 0)
                {
                    return null;
                }
                else
                {
                    var result = new PaymentDBMySQL
                    {
                        id = id.First(),
                        minerId = minerId,
                        txId = txId,
                        value = value,
                        fee = fee,
                        timeStamp = timeStamp,
                        verified = false,
                        paymentSession = paymentSession
                    };
                    return result;
                }
            }
        }

        public List<PaymentDBType> getUnverifiedPayments()
        {
            var oldestTimeStamp = DateTime.Now - TimeSpan.FromDays(1);
            using (var db = new MySqlConnection(Config.dbConnectionString))
            {
                return db.Query<PaymentDBMySQL>("SELECT * FROM Payment WHERE verified = 0 AND `timeStamp` > @timeStamp", new { timeStamp = oldestTimeStamp }).ToList().ConvertAll(x => (PaymentDBType)x);
            }
        }

        public void setPaymentVerified(int paymentId)
        {
            using (var db = new MySqlConnection(Config.dbConnectionString))
            {
                db.Execute("UPDATE Payment SET verified = 1 WHERE id = @paymentId", new { paymentId = paymentId });
            }
        }

        public bool shareExists(string nonce)
        {
            using (var db = new MySqlConnection(Config.dbConnectionString))
            {
                var shareId = db.Query<int>("SELECT id FROM Share WHERE nonce = @nonce", new { nonce = nonce });
                return shareId.Count() > 0 && shareId.First() > 0;
            }
        }

        public void getActiveMinersCount(out int activeMinersCount, out int activeWorkersCount)
        {
            var limit = DateTime.Now - (new TimeSpan(0, 5, 0));
            using (var db = new MySqlConnection(Config.dbConnectionString))
            {
                var recs = db.QueryMultiple(@"SELECT COUNT(*) FROM Miner WHERE lastSeen > @limit;
                                              SELECT COUNT(*) FROM Worker WHERE lastSeen > @limit;",
                                              new { limit = limit });

                activeMinersCount = recs.ReadSingleOrDefault<int>();
                activeWorkersCount = recs.ReadSingleOrDefault<int>();
            }
        }

        public decimal getTotalPayments()
        {
            using (var db = new MySqlConnection(Config.dbConnectionString))
            {
                var result = db.QuerySingleOrDefault<decimal?>("SELECT SUM(value) FROM Payment WHERE verified = 1");
                return result.HasValue ? result.Value : 0;
            }
        }

        public double getTotalHashrate()
        {
            var limit = DateTime.Now - (new TimeSpan(0, 5, 0));
            using (var db = new MySqlConnection(Config.dbConnectionString))
            {
                var result = db.QuerySingleOrDefault<double?>("SELECT SUM(hashrate) FROM Worker WHERE lastSeen > @limit", new { limit = limit });
                return result.HasValue ? result.Value : 0;
            }
        }

        public int getBlocksMinedSince(DateTime since)
        {
            using (var db = new MySqlConnection(Config.dbConnectionString))
            {
                return db.QuerySingleOrDefault<int>("SELECT COUNT(*) FROM PoolBlock WHERE miningStart > @since AND resolution = 2", new { since = since });
            }
        }

        public List<MinerData> getMinersDataForLast(int hours)
        {
            var limitMiner = DateTime.Now - (new TimeSpan(hours, 0, 0));
            var limitWorker = DateTime.Now - (new TimeSpan(0, 5, 0));

            using (var db = new MySqlConnection(Config.dbConnectionString))
            {
                return db.Query<MinerData>(@"SELECT Miner.address AS Address, Miner.lastSeen AS LastSeen, Miner.pending AS Pending, 
                (SELECT SUM(Worker.hashrate) FROM Worker WHERE Worker.minerId = Miner.id AND Worker.lastSeen > @limitWorker) AS HashRate,
                (SELECT COUNT(Share.id) FROM Share WHERE Share.minerId = Miner.Id AND Share.processed = 0) AS RoundShares
                FROM Miner
                WHERE Miner.lastSeen > @limitMiner
                ORDER BY RoundShares DESC, Miner.lastSeen DESC", new { limitWorker = limitWorker, limitMiner = limitMiner }).ToList();
            }
        }

        public List<BlockData> getMinedBlocks()
        {
            var address = new Address(IxianHandler.getWalletStorage().getPrimaryAddress()).ToString();

            using (var db = new MySqlConnection(Config.dbConnectionString))
            {
                return db.Query<BlockData>(@"SELECT DISTINCT PoolBlock.blockNum AS BlockNum, PoolBlock.miningEnd AS `TimeStamp`, PowData.reward AS Reward,
                    IF(ISNULL(PowData.id), 'Unconfirmed', 'Confirmed') AS Status, Miner.address AS MinerAddress
                FROM PoolBlock
            	    LEFT JOIN PowData ON PowData.solvedBlock = PoolBlock.blockNum AND PowData.solverAddress = @address
	                LEFT JOIN Share ON Share.blockNum = PoolBlock.blockNum AND Share.blockResolved = 1
                        AND Share.`timeStamp` = (SELECT MIN(Share.`timeStamp`) FROM Share WHERE Share.blockNum = PoolBlock.blockNum AND Share.blockResolved = 1)
	                LEFT JOIN Miner ON Miner.id = Share.minerId
	            WHERE PoolBlock.resolution = 2
	            ORDER BY PoolBlock.miningEnd DESC
                LIMIT 1000", new { address = address }).ToList();
            }
        }

        public List<PaymentData> getPayments()
        {
            using (var db = new MySqlConnection(Config.dbConnectionString))
            {
                return db.Query<PaymentData>(@"SELECT IF(Payment.minerId = -1, 'Pool Fee', Miner.address) AS MinerAddress, Payment.`timeStamp` AS `TimeStamp`, Payment.value AS Value, 
        	    	Payment.txId AS TxId, IF(Payment.verified = 1, 'Verified', 'Pending') AS Status
                FROM Payment
		            LEFT JOIN Miner ON Miner.id = Payment.minerId
                ORDER BY Payment.`timeStamp` DESC
                LIMIT 1000").ToList();
            }
        }

        public List<PoolStateDBType> getAllPoolStates()
        {
            using (var db = new MySqlConnection(Config.dbConnectionString))
            {
                return db.Query<PoolStateDBMySQL>("SELECT * FROM PoolState").ToList().ConvertAll(x => (PoolStateDBType)x);
            }
        }

        public PoolStateDBType setPoolState(string key, string value)
        {
            using (var db = new MySqlConnection(Config.dbConnectionString))
            {
                db.Execute(@"INSERT INTO PoolState(`key`, value)
                        VALUES(@key, @value)
                        ON DUPLICATE KEY UPDATE value = @value", new { key = key, value = value });
            }

            return new PoolStateDBMySQL
            {
                key = key,
                value = value
            };
        }

        public double getMinerHashrate(string address)
        {
            var limit = DateTime.Now - (new TimeSpan(0, 5, 0));
            using (var db = new MySqlConnection(Config.dbConnectionString))
            {
                var result = db.QuerySingleOrDefault<double?>(@"SELECT SUM(Worker.hashrate) AS value 
                                                        FROM Worker
                                                            JOIN Miner ON Worker.minerId = Miner.id AND Miner.address = @address
                                                        WHERE Worker.lastSeen > @limit", new { address = address, limit = limit });
                return result.HasValue ? result.Value : 0;
            }
        }

        public int getMinerWorkersCount(string address)
        {
            var limit = DateTime.Now - (new TimeSpan(0, 5, 0));
            using (var db = new MySqlConnection(Config.dbConnectionString))
            {
                return db.QuerySingleOrDefault<int>(@"SELECT COUNT(Worker.id) AS value 
                                                    FROM Worker
                                                        JOIN Miner ON Worker.minerId = Miner.id AND Miner.address = @address
                                                     WHERE Worker.lastSeen > @limit", new { address = address, limit = limit });
            }
        }

        public decimal getMinerTotalPayments(string address)
        {
            using (var db = new MySqlConnection(Config.dbConnectionString))
            {
                var result = db.QuerySingleOrDefault<decimal?>(@"SELECT SUM(Payment.value) AS value 
                                                                FROM Payment
                                                                    JOIN Miner ON Payment.minerId = Miner.id AND Miner.address = @address",
                                                                    new { address = address });
                return result.HasValue ? result.Value : 0;
            }
        }

        public decimal getMinerPendingValue(string address)
        {
            using (var db = new MySqlConnection(Config.dbConnectionString))
            {
                var result = db.QuerySingleOrDefault<decimal?>("SELECT pending FROM Miner WHERE address = @address", new { address = address });
                return result.HasValue ? result.Value : 0;
            }
        }

        public List<MinerWorker> getMinerWorkersInformation(string address)
        {
            var limit = DateTime.Now - (new TimeSpan(0, 5, 0));

            using (var db = new MySqlConnection(Config.dbConnectionString))
            {
                return db.Query<MinerWorker>(@"SELECT Worker.name AS Name, Worker.hashrate AS Hashrate,
                (SELECT COUNT(Share.id) FROM Share WHERE Share.workerId = Worker.id AND Share.processed = 0) AS Shares, Worker.lastSeen AS LastSeen
                    FROM Worker
                        JOIN Miner ON Miner.id = Worker.minerId AND Miner.address = @address
                 WHERE Worker.lastSeen > @limit ORDER BY Worker.lastSeen DESC", new { address = address, limit = limit }).ToList();
            }
        }

        public List<MinerPayment> getMinerPaymentsInformation(string address)
        {
            using (var db = new MySqlConnection(Config.dbConnectionString))
            {
                return db.Query<MinerPayment>(@"SELECT Payment.txId AS TxId, Payment.`timeStamp` AS `TimeStamp`, Payment.value AS Value, IF(Payment.verified = 1, 'Verified', 'Pending') AS Status
                                            FROM Payment
                                                JOIN Miner ON Payment.minerId = Miner.id AND Miner.address = @address
                                            ORDER BY Payment.`timeStamp` DESC
                                            LIMIT 1000", new { address = address }).ToList();
            }
        }

        public List<MinerDBType> getMinersWithPendingBalance()
        {
            using (var db = new MySqlConnection(Config.dbConnectionString))
            {
                return db.Query<MinerDBSQLite>("SELECT * FROM Miner WHERE pending > 0").ToList().ConvertAll(x => (MinerDBType)x);
            }
        }

        public int addNotification(int type, string notification, bool active)
        {
            var entry = new NotificationDBSQLite
            {
                type = type,
                notification = notification,
                active = active
            };

            using (var db = new MySqlConnection(Config.dbConnectionString))
            {
                var recs = db.QueryMultiple("INSERT INTO Notification(`type`, notification, active) VALUES(@type, @notification, @active); SELECT LAST_INSERT_ID();", entry);
                var id = recs.Read<int>();
                if (id.Count() == 0)
                {
                    return -1;
                }
                else
                {
                    return id.First();
                }
            }
        }

        public void updateNotificationStatus(int id, bool status)
        {
            using (var db = new MySqlConnection(Config.dbConnectionString))
            {
                db.Execute("UPDATE Notification SET active = @status WHERE id = @id", new { id = id, status = status });
            }
        }

        public List<NotificationDBType> getActiveNotifications()
        {
            using (var db = new MySqlConnection(Config.dbConnectionString))
            {
                return db.Query<NotificationDBMySQL>("SELECT * FROM Notification WHERE active = 1 ORDER BY id").ToList().ConvertAll(x => (NotificationDBType)x);
            }
        }
    }
}
