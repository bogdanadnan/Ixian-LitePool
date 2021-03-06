using System;
using System.Collections.Generic;
using System.Linq;
using LP.DB;
using LP.Meta;
using LP.Network;
using static LP.DB.PoolDB;

namespace LP.Pool
{
    public enum MiningBlockResolution
    {
        Mining = 0,
        TimedOut = 1,
        SolvedByPool = 2,
        SolveByOther = 3,
        EvictedFromRedactedWindow = 4
    }

    public enum NotificationType
    {
        Primary = 0,
        Info = 1,
        Success = 2,
        Warning = 3,
        Danger = 4
    }

    public class NotificationData
    {
        public string Type { get; set; }
        public string Notification { get; set; }
    }

    public class ActivePoolBlock : RepositoryBlock
    {
        public ActivePoolBlock(RepositoryBlock blk)
        {
            this.blockNum = blk.blockNum;
            this.difficulty = blk.difficulty;
            this.version = blk.version;
            this.blockChecksum = blk.blockChecksum;
            this.timeStamp = blk.timeStamp;

            this.miningStart = DateTime.Now;
            this.miningEnd = null;
            this.resolution = MiningBlockResolution.Mining;
        }

        public ActivePoolBlock(ActivePoolBlock blk)
        {
            this.blockNum = blk.blockNum;
            this.difficulty = blk.difficulty;
            this.version = blk.version;
            this.blockChecksum = blk.blockChecksum;
            this.timeStamp = blk.timeStamp;

            this.miningStart = blk.miningStart;
            this.miningEnd = blk.miningEnd;
            this.resolution = blk.resolution;
        }

        public DateTime? miningStart { get; set; }

        public DateTime? miningEnd { get; set; }

        public MiningBlockResolution resolution { get; set; }
    }

    public class Pool
    {
        private static Pool instance = null;

        public static Pool Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Pool();
                }

                return instance;
            }
        }

        private object activePoolBlockLock = new object();
        private ActivePoolBlock activePoolBlock = null;
        private List<ulong> last5ActiveBlocks = new List<ulong>();

        private ulong adjustedDifficulty = Config.startingDifficulty;
        private ulong blockDifficulty = ulong.MaxValue;

        public Pool()
        {
            var diff = State.Instance.get("PoolDifficulty");
            if (!ulong.TryParse(diff, out adjustedDifficulty) || adjustedDifficulty == 0)
            {
                adjustedDifficulty = Config.startingDifficulty;
            }
        }

        public ulong getDifficulty()
        {
            return Math.Min(adjustedDifficulty, blockDifficulty);
        }

        public void updateSharesPerSecond(double shrrt)
        {
            if (shrrt < (Config.targetSharesPerSecond - 1))
            {
                if (adjustedDifficulty > Config.diffStep)
                {
                    adjustedDifficulty -= Config.diffStep;
                    State.Instance.set("PoolDifficulty", adjustedDifficulty.ToString());
                }
            }
            else if (shrrt > (Config.targetSharesPerSecond + 1))
            {
                adjustedDifficulty += Config.diffStep;
                State.Instance.set("PoolDifficulty", adjustedDifficulty.ToString());
            }
        }

        public void setDifficulty(ulong diff)
        {
            adjustedDifficulty = diff;
            State.Instance.set("PoolDifficulty", diff.ToString());
        }

        public void resetActiveBlock(MiningBlockResolution resolution, ulong targetBlockNum)
        {
            lock (activePoolBlockLock)
            {
                PoolBlockDBType blk = PoolDB.Instance.getPoolBlock((long)targetBlockNum);

                if (blk != null)
                {
                    PoolDB.Instance.updatePoolBlock(blk.blockNum,
                        Math.Max(blk.resolution, (int)resolution),
                        (!blk.miningEnd.HasValue || (activePoolBlock != null && activePoolBlock.blockNum == targetBlockNum)) ? DateTime.Now : blk.miningEnd.Value);
                }

                if (activePoolBlock != null && activePoolBlock.blockNum == targetBlockNum)
                {
                    activePoolBlock = null;
                    APIServer.Instance.resetCache();
                }
            }
        }

        public ActivePoolBlock getActiveBlock()
        {
            lock (activePoolBlockLock)
            {
                if (activePoolBlock != null)
                {
                    return new ActivePoolBlock(activePoolBlock);
                }
                else
                {
                    return null;
                }
            }
        }

        internal void setActiveBlock(ActivePoolBlock blk)
        {
            lock (activePoolBlockLock)
            {
                PoolDB.Instance.addPoolBlock((long)blk.blockNum, (long)getDifficulty());

                activePoolBlock = blk;
                blockDifficulty = blk.difficulty;

                last5ActiveBlocks.Add(activePoolBlock.blockNum);
                while (last5ActiveBlocks.Count > 5)
                {
                    last5ActiveBlocks.RemoveAt(0);
                }
            }
        }

        public bool checkDuplicateShare(string nonce)
        {
            return PoolDB.Instance.shareExists(nonce);
        }

        public bool isMinedBlock(ulong blockNum)
        {
            lock (activePoolBlockLock)
            {
                return last5ActiveBlocks.Any(b => b == blockNum);
            }
        }

        public static int getTotalHashrate()
        {
            double hr = PoolDB.Instance.getTotalHashrate();
            return (int)Math.Floor(hr / 1000.0);
        }

        public static int getBlocksMinedInLast24h()
        {
            return PoolDB.Instance.getBlocksMinedSince(DateTime.Now - (new TimeSpan(24, 0, 0)));
        }

        public static List<BlockData> getMinedBlocks()
        {
            return PoolDB.Instance.getMinedBlocks();
        }

        public int addNotification(NotificationType type, string notification, bool active)
        {
            return PoolDB.Instance.addNotification((int)type, notification, active);
        }

        public void updateNotificationStatus(int id, bool status)
        {
            PoolDB.Instance.updateNotificationStatus(id, status);
        }

        public List<NotificationData> getActiveNotifications()
        {
            return PoolDB.Instance.getActiveNotifications().ConvertAll(n => new NotificationData
            {
                Type = ((NotificationType)n.type).ToString().ToLower(),
                Notification = n.notification
            });
        }
    }
}
