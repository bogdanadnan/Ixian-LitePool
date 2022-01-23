using System;
using System.Collections.Generic;
using LP.Meta;

namespace LP.DB
{
    public interface MinerDBType
    {
        int id { get; set; }
        string address { get; set; }
        DateTime lastSeen { get; set; }
        decimal pending { get; set; }
    }

    public interface WorkerDBType
    {
        int id { get; set; }
        int minerId { get; set; }
        string name { get; set; }
        string miningApp { get; set; }
        double hashrate { get; set; }
        DateTime lastSeen { get; set; }
    }

    public interface ShareDBType
    {
        int id { get; set; }
        int minerId { get; set; }
        int workerId { get; set; }
        DateTime timeStamp { get; set; }
        long blockNum { get; set; }
        long difficulty { get; set; }
        string nonce { get; set; }
        bool blockResolved { get; set; }
        bool processed { get; set; }
        string paymentSession { get; set; }
    }

    public interface BlockDBType
    {
        long blockNum { get; set; }
        long difficulty { get; set; }
        int version { get; set; }
        byte[] checksum { get; set; }
        DateTime timeStamp { get; set; }
    }

    public interface PoolBlockDBType
    {
        long blockNum { get; set; }
        DateTime miningStart { get; set; }
        DateTime? miningEnd { get; set; }
        int resolution { get; set; }
        long poolDifficulty { get; set; }
    }

    public interface PowDataDBType
    {
        int id { get; set; }
        long blockNum { get; set; }
        long solvedBlock { get; set; }
        string solverAddress { get; set; }
        string txId { get; set; }
        decimal reward { get; set; }
    }

    public interface PaymentDBType
    {
        int id { get; set; }
        int minerId { get; set; }
        DateTime timeStamp { get; set; }
        decimal value { get; set; }
        decimal fee { get; set; }
        string txId { get; set; }
        bool verified { get; set; }
        string paymentSession { get; set; }
    }

    public interface PoolStateDBType
    {
        int id { get; set; }
        string key { get; set; }
        string value { get; set; }
    }

    public interface NotificationDBType
    {
        int id { get; set; }
        int type { get; set; }
        string notification { get; set; }
        bool active { get; set; }
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

    public class StringData
    {
        public string value { get; set; }
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

    public interface IPoolDB
    {
        void addBlock(long blockNum, int version, long difficulty, DateTime timeStamp, byte[] blockChecksum);
        int addNotification(int type, string notification, bool active);
        PaymentDBType addPayment(int minerId, string txId, decimal value, decimal fee, DateTime timeStamp, string paymentSession);
        void addPowDataForBlock(long blockNum, decimal reward, long solvedBlock, string solverAddress, string txId);
        int addShare(ShareDBType share);
        void cleanUpBlocks(long blockLimit);
        void cleanUpShares();
        ShareDBType createShare(long blocknum, long difficulty, string nonce, bool blockResolved, int minerId, int workerId);
        void deletePowDataFromBlock(long blkNum, long minedBlkNum);
        void getActiveMinersCount(out int activeMinersCount, out int activeWorkersCount);
        List<NotificationDBType> getActiveNotifications();
        List<PoolStateDBType> getAllPoolStates();
        BlockDBType getBlock(long blkNum);
        int getBlocksMinedSince(DateTime since);
        List<BlockData> getMinedBlocks();
        MinerDBType getMiner(string address);
        double getMinerHashrate(string address);
        List<MinerPayment> getMinerPaymentsInformation(string address);
        decimal getMinerPendingValue(string address);
        List<MinerDBType> getMiners(List<int> minerIds);
        List<MinerData> getMinersDataForLast(int hours);
        List<MinerDBType> getMinersWithPendingBalance();
        decimal getMinerTotalPayments(string address);
        int getMinerWorkersCount(string address);
        List<MinerWorker> getMinerWorkersInformation(string address);
        List<PaymentData> getPayments();
        PoolBlockDBType getPoolBlock(long blkNum);
        List<PowDataDBType> getPowDataFromBlock(long blkNum);
        double getTotalHashrate();
        decimal getTotalPayments();
        List<ShareDBType> getUnprocessedShares();
        List<ShareDBType> getUnprocessedShares(string paymentSession);
        List<PaymentDBType> getUnverifiedPayments();
        WorkerDBType getWorker(int minerId, string workerName);
        PoolStateDBType setPoolState(string key, string value);
        bool shareExists(string nonce);
        int updateMiner(MinerDBType miner);
        void updateMinerPendingBalance(int minerId, decimal pending);
        void updateNotificationStatus(int id, bool status);
        void setPaymentVerified(int paymentId);
        void addPoolBlock(long blockNum, long difficulty);
        void updatePoolBlock(long blockNum, int blkResolution, DateTime miningEnd);
        int updateWorker(WorkerDBType worker);
    }

    public static class PoolDB
    {
        private static IPoolDB instance = null;

        public static IPoolDB Instance
        {
            get
            {
                if (instance == null)
                {
                    switch(Config.dbProvider.ToLower())
                    {
                        case "sqlite":
                            instance = new PoolSQLiteDB();
                            break;
                        case "mysql":
                            instance = new PoolMySQLDB();
                            break;
                        default:
                            instance = new PoolSQLiteDB();
                            break;
                    }
                }

                return instance;
            }
        }
    }
}