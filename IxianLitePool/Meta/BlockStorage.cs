using System;
using System.Collections.Generic;
using LP.DB;
using static LP.DB.PoolDB;

namespace LP.Meta
{
    public class BlockStorage
    {
        private static BlockStorage instance = null;
        public static BlockStorage Instance
        {
            get
            {
                if(instance == null)
                {
                    instance = new BlockStorage();
                }

                return instance;
            }
        }

        public bool hasBlockInStorage(ulong blkNum)
        {
            return PoolDB.Instance.getBlock((long)blkNum) != null;
        }

        public RepositoryBlock getBlock(ulong blkNum)
        {
            BlockDBType blk = PoolDB.Instance.getBlock((long)blkNum);

            if(blk == null)
            {
                return null;
            }

            return new RepositoryBlock
            {
                blockNum = blkNum,
                version = blk.version,
                difficulty = (ulong)blk.difficulty,
                blockChecksum = blk.checksum,
                timeStamp = blk.timeStamp
            };
        }

        public List<BlockSolver> getBlockSolversFromBlock(ulong blkNum)
        {
            List<BlockSolver> solvers = new List<BlockSolver>();
            List<PowDataDBType> powDataDBs = PoolDB.Instance.getPowDataFromBlock((long)blkNum);

            return powDataDBs.ConvertAll(pow => new BlockSolver
            {
                blockNum = (ulong)pow.solvedBlock,
                solverAddress = pow.solverAddress,
                txId = pow.txId,
                reward = pow.reward
            });
        }

        public void addBlock(RepositoryBlock blk)
        {
            PoolDB.Instance.addBlock((long)blk.blockNum, blk.version, (long)blk.difficulty, blk.timeStamp, blk.blockChecksum);
        }

        public void updateBlockSolvers(ulong blockNum, ulong minedBlockNum, List<BlockSolver> blockSolvers)
        {
            PoolDB.Instance.deletePowDataFromBlock((long)blockNum, (long)minedBlockNum);
            foreach (var solver in blockSolvers)
            {
                PoolDB.Instance.addPowDataForBlock((long)blockNum, solver.reward, (long)solver.blockNum, solver.solverAddress, solver.txId);
            }
        }

        public void cleanUpBlocks(ulong limit)
        {
            PoolDB.Instance.cleanUpBlocks((long)limit);
        }
    }
}
