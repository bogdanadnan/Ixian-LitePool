using System;
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
                if(instance == null)
                {
                    instance = new Pool();
                }

                return instance;
            }
        }

        private object activePoolBlockLock = new object();
        private ActivePoolBlock activePoolBlock = null;

        private ulong adjustedDifficulty = Config.startingDifficulty;

        public ulong getDifficulty()
        {
            return adjustedDifficulty;
        }

        public void updateSharesPerSecond(int shrrt)
        {
            if(shrrt < Config.targetSharesPerSecond - 1)
            {
                if (adjustedDifficulty > 1000)
                {
                    adjustedDifficulty -= 1000;
                }
            }
            else if(shrrt > Config.targetSharesPerSecond + 1)
            {
                adjustedDifficulty += 1000;
            }
        }

        public void resetActiveBlock(MiningBlockResolution resolution)
        {
            lock(activePoolBlockLock)
            {
                if (activePoolBlock != null)
                {
                    PoolBlockDBType blk = PoolDB.Instance.getPoolBlock((long)activePoolBlock.blockNum);
                    if(blk != null)
                    {
                        blk.miningEnd = DateTime.Now;
                        blk.resolution = (int)resolution;
                        PoolDB.Instance.updatePoolBlock(blk);
                    }
                }
                activePoolBlock = null;
                APIServer.Instance.resetCache();
            }
        }

        public ActivePoolBlock getActiveBlock()
        {
            lock(activePoolBlockLock)
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
            lock(activePoolBlockLock)
            {
                PoolDB.Instance.updatePoolBlock(new PoolBlockDBType
                {
                    blockNum = (long)blk.blockNum,
                    miningStart = blk.miningStart.HasValue ? blk.miningStart.Value : DateTime.Now,
                    miningEnd = null,
                    poolDifficulty = (long)getDifficulty(),
                    resolution = (int)blk.resolution
                });
                activePoolBlock = blk;
            } 
        }

        public bool checkDuplicateShare(string nonce)
        {
            return PoolDB.Instance.shareExists(nonce);
        }

        public static int getTotalHashrate()
        {
            double hr = PoolDB.Instance.getTotalHashrate();
            return (int)Math.Floor(hr / 1000.0);
        }
    }
}
