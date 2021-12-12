using IXICore;
using IXICore.Meta;
using IXICore.Network;
using IXICore.Utils;
using LP.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using IXICore.Inventory;

namespace LP.Meta
{
    public class Balance
    {
        public byte[] address = null;
        public IxiNumber balance = 0;
        public ulong blockHeight = 0;
        public byte[] blockChecksum = null;
        public bool verified = false;
    }

    public class RequestData
    {
        public DateTime timeStamp = DateTime.Now;
        public uint retryCount = 0;
        public RemoteEndpoint endpoint = null;
        public ulong blockNum = 0;
        public Block block = null;
        public List<Transaction> transactions = new List<Transaction>();
    }

    public class Node : IxianNode
    {
        private static uint blockRequestTimeout = 30;
        private static uint maxRetryCount = 10;
        private Thread updater = null;
        private bool updaterRunning = false;
        private ulong networkBlockHeight = 0;

        private Dictionary<byte[], ulong> transactionMapping = new Dictionary<byte[], ulong>(new ByteArrayComparer());
        private Dictionary<ulong, Block> blockRepository = new Dictionary<ulong, Block>();

        private Dictionary<ulong, RequestData> requestsQueue = new Dictionary<ulong, RequestData>();
        private object currentRequestLock = new object();
        private RequestData currentRequest = null;

        private Dictionary<ulong, List<Transaction>> knownSolvedBlocks = new Dictionary<ulong, List<Transaction>>();
        private List<Block> localSolvedBlocks = new List<Block>();

        private object activePoolBlockLock = new object();
        private Block activePoolBlock = null;

        public static bool running = false;

        public static Balance balance = new Balance();      // Stores the last known balance for this node

        internal static TransactionInclusion tiv = null;

        private bool generatedNewWallet = false;


        public Node()
        {
            IxianHandler.init(Config.version, this, NetworkType.main);
            init();
        }

        // Perform basic initialization of node
        private void init()
        {
            Logging.consoleOutput = false;

            running = true;

            // Load or Generate the wallet
            if (!initWallet())
            {
                running = false;
                IxianLitePool.Program.running = false;
                return;
            }

            Console.WriteLine("Connecting to Ixian network...");

            PeerStorage.init("");

            // Init TIV
            tiv = new TransactionInclusion();

            updaterRunning = true;
            updater = new Thread(this.updaterThread);
            updater.Start();
        }

        private void updaterThread()
        {
            while (updaterRunning)
            {
                Thread.Sleep(500);

                lock (currentRequestLock)
                {
                    if (currentRequest == null)
                    {
                        lock (requestsQueue)
                        {
                            if (requestsQueue.Count > 0)
                            {
                                currentRequest = requestsQueue[requestsQueue.Keys.Min()];
                                requestsQueue.Remove(requestsQueue.Keys.Min());
                                currentRequest.timeStamp = DateTime.Now;
                                Console.WriteLine("Requesting block {0}", currentRequest.blockNum);
                                currentRequest.endpoint = broadcastGetBlock(currentRequest.blockNum, null, (currentRequest.endpoint != null && currentRequest.endpoint.isConnected()) ? currentRequest.endpoint : null, 0, true);
                            }
                        }
                    }
                    if (currentRequest == null) // no new requests in requests queue, get an older block
                    {
                        lock (blockRepository)
                        {
                            if (blockRepository.Count > 0 && blockRepository.Count < (long)ConsensusConfig.getRedactedWindowSize() - 10)
                            {
                                ulong lowestBlockNum = blockRepository.Keys.Min() - 1;
                                currentRequest = new RequestData
                                {
                                    blockNum = lowestBlockNum
                                };
                                currentRequest.timeStamp = DateTime.Now;
                                Console.WriteLine("Requesting block {0}", currentRequest.blockNum);
                                currentRequest.endpoint = broadcastGetBlock(currentRequest.blockNum, null, null, 0, true);
                            }
                        }
                    }

                    if (currentRequest == null) // nothing to ask for, bail out
                    {
                        continue;
                    }

                    if ((DateTime.Now - currentRequest.timeStamp).Seconds > blockRequestTimeout)
                    {
                        if (currentRequest.retryCount < maxRetryCount)
                        {
                            Console.WriteLine("Request for block {0} timed out, requesting {1} again.", currentRequest.blockNum, currentRequest.block != null ? "transactions" : "block");
                            currentRequest.timeStamp = DateTime.Now;
                            currentRequest.endpoint = broadcastGetBlock(currentRequest.blockNum, currentRequest.endpoint, null, (currentRequest.block != null ? 1 : 0), currentRequest.block == null);
                        }
                        else // reset request completely and try again
                        {
                            if (currentRequest.block == null) // no request containing the block has been received, there is no point in trying again
                            {
                                Console.WriteLine("Request for block {0} exceeded max retries with no data received, bailing out.", currentRequest.blockNum);
                                currentRequest = null;
                            }
                            else
                            {
                                Console.WriteLine("Request for block {0} exceeded max retries with some data received, resetting and trying again.", currentRequest.blockNum);
                                // reset transaction mapping and request data and try again
                                lock (transactionMapping)
                                {
                                    var toRemove = transactionMapping.Where(tm => tm.Value == currentRequest.blockNum);
                                    foreach (var tm in toRemove)
                                    {
                                        transactionMapping.Remove(tm.Key);
                                    }
                                }

                                currentRequest.block = null;
                                currentRequest.transactions.Clear();
                                currentRequest.timeStamp = DateTime.Now;
                                currentRequest.endpoint = broadcastGetBlock(currentRequest.blockNum, null, null, 0, true);
                            }
                        }
                    }
                }
            }
        }

        private bool initWallet()
        {
            WalletStorage walletStorage = new WalletStorage(Config.walletFile);

            Logging.flush();

            if (!walletStorage.walletExists())
            {
                ConsoleHelpers.displayBackupText();

                // Request a password
                string password = "";
                while (password.Length < 10)
                {
                    Logging.flush();
                    password = ConsoleHelpers.requestNewPassword("Enter a password for your new wallet: ");
                    if (IxianHandler.forceShutdown)
                    {
                        return false;
                    }
                }
                walletStorage.generateWallet(password);
                generatedNewWallet = true;
            }
            else
            {
                ConsoleHelpers.displayBackupText();

                bool success = false;
                while (!success)
                {

                    string password = "";
                    if (password.Length < 10)
                    {
                        Logging.flush();
                        Console.Write("Enter wallet password: ");
                        password = ConsoleHelpers.getPasswordInput();
                    }
                    if (IxianHandler.forceShutdown)
                    {
                        return false;
                    }
                    if (walletStorage.readWallet(password))
                    {
                        success = true;
                    }
                }
            }


            if (walletStorage.getPrimaryPublicKey() == null)
            {
                return false;
            }

            // Wait for any pending log messages to be written
            Logging.flush();

            Console.WriteLine();
            Console.WriteLine("Your IXIAN addresses are: ");
            Console.ForegroundColor = ConsoleColor.Green;
            foreach (var entry in walletStorage.getMyAddressesBase58())
            {
                Console.WriteLine(entry);
            }
            Console.ResetColor();
            Console.WriteLine();

            if (walletStorage.viewingWallet)
            {
                Logging.error("Viewing-only wallet {0} cannot be used as the primary DLT Node wallet.", Base58Check.Base58CheckEncoding.EncodePlain(walletStorage.getPrimaryAddress()));
                return false;
            }

            IxianHandler.addWallet(walletStorage);

            return true;
        }

        public void stop()
        {
            updaterRunning = false;
            updater.Join();

            IxianHandler.forceShutdown = true;

            // Stop TIV
            tiv.stop();

            // Stop the network queue
            NetworkQueue.stop();

            // Stop all network clients
            NetworkClientManager.stop();
        }

        public void start()
        {
            PresenceList.init(IxianHandler.publicIP, 0, 'M');
            NetworkUtils.configureNetwork("", 10234);

            // Start the network queue
            NetworkQueue.start();
            NetworkServer.beginNetworkOperations();

            // Start the network client manager
            NetworkClientManager.start(2);

            // Start TIV
            if (generatedNewWallet || !File.Exists(Config.walletFile))
            {
                generatedNewWallet = false;
                tiv.start("");
            }
            else
            {
                tiv.start("", 0, null);
            }
        }

        static public void getBalance()
        {
            ProtocolMessage.setWaitFor(ProtocolMessageCode.balance2);

            // Return the balance for the matching address
            using (MemoryStream mw = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(mw))
                {
                    writer.WriteIxiVarInt(IxianHandler.getWalletStorage().getPrimaryAddress().Length);
                    writer.Write(IxianHandler.getWalletStorage().getPrimaryAddress());
                    NetworkClientManager.broadcastData(new char[] { 'M', 'H' }, ProtocolMessageCode.getBalance2, mw.ToArray(), null);
                }
            }
            ProtocolMessage.wait(30);
        }

        static public void sendTransaction(string address, IxiNumber amount)
        {
            Node.getBalance();

            if (Node.balance.balance < amount)
            {
                Console.WriteLine("Insufficient funds.\n");
                return;
            }

            SortedDictionary<byte[], IxiNumber> to_list = new SortedDictionary<byte[], IxiNumber>(new ByteArrayComparer());

            IxiNumber fee = ConsensusConfig.transactionPrice;
            byte[] from = IxianHandler.getWalletStorage().getPrimaryAddress();
            byte[] pubKey = IxianHandler.getWalletStorage().getPrimaryPublicKey();
            to_list.AddOrReplace(Base58Check.Base58CheckEncoding.DecodePlain(address), amount);
            Transaction transaction = new Transaction((int)Transaction.Type.Normal, fee, to_list, from, null, pubKey, IxianHandler.getHighestKnownNetworkBlockHeight());
            if (IxianHandler.addTransaction(transaction, true))
            {
                Console.WriteLine("Sending transaction, txid: {0}\n", Transaction.txIdV8ToLegacy(transaction.id));
            } else
            {
                Console.WriteLine("Could not send transaction\n");
            }

        }

        public void setNetworkBlock(ulong block_height, RemoteEndpoint endpoint)
        {
            if (networkBlockHeight < block_height)
            {
                processNewBlockHeight(block_height, endpoint);
            }
        }

        public override void receivedTransactionInclusionVerificationResponse(byte[] txid, bool verified)
        {
            string status = "NOT VERIFIED";
            if (verified)
            {
                status = "VERIFIED";
                PendingTransactions.remove(txid);
            }
            Console.WriteLine("Transaction {0} is {1}\n", Transaction.txIdV8ToLegacy(txid), status);
        }

        public override void receivedBlockHeader(BlockHeader block_header, bool verified)
        {
            if (balance.blockChecksum != null && balance.blockChecksum.SequenceEqual(block_header.blockChecksum))
            {
                balance.verified = true;
            }
            if (block_header.blockNum >= networkBlockHeight)
            {
                IxianHandler.status = NodeStatus.ready;
                setNetworkBlock(block_header.blockNum, null);
            }
            processPendingTransactions();
        }

        public override ulong getLastBlockHeight()
        {
            lock (blockRepository)
            {
                if (blockRepository.Count > 0)
                {
                    return blockRepository.Keys.Max();
                }
                else
                {
                    return 0;
                }
            }
        }

        public override bool isAcceptingConnections()
        {
            return true;
        }

        public override ulong getHighestKnownNetworkBlockHeight()
        {
            return networkBlockHeight;
        }

        public override int getLastBlockVersion()
        {
            if (tiv.getLastBlockHeader() == null || tiv.getLastBlockHeader().version < Block.maxVersion)
            {
                return Block.maxVersion - 1;
            }
            return tiv.getLastBlockHeader().version;
        }

        public override bool addTransaction(Transaction tx, bool force_broadcast)
        {
            // TODO Send to peer if directly connectable
            CoreProtocolMessage.broadcastProtocolMessage(new char[] { 'M', 'H' }, ProtocolMessageCode.transactionData, tx.getBytes(), null);
            PendingTransactions.addPendingLocalTransaction(tx);
            return true;
        }

        public void handleBlockData(byte[] data, RemoteEndpoint endpoint)
        {
            Block block = new Block(data);
            if (endpoint.blockHeight < block.blockNum)
            {
                endpoint.blockHeight = block.blockNum;
            }

            Console.WriteLine("Received new block data for block {0}", block.blockNum);

            if (networkBlockHeight < block.blockNum)
            {
                processNewBlockHeight(block.blockNum, endpoint);
            }
            else
            {
                lock (currentRequestLock)
                {
                    if (currentRequest != null && (currentRequest.blockNum == block.blockNum))
                    {
                        currentRequest.timeStamp = DateTime.Now;

                        if (currentRequest.block == null)
                        {
                            currentRequest.block = block;

                            if (block.transactions.Count == 0 ||
                                (block.transactions.Count == 1 && block.transactions.First()[0] == 0))
                            {
                                Console.WriteLine("Block {0} doesn't have transactions, adding to repo.", block.blockNum);
                                finalizeRequest();
                            }
                            else
                            {
                                foreach (var txId in block.transactions)
                                {
                                    if (txId[0] == 0) // ignore staking rewards
                                    {
                                        continue;
                                    }

                                    if (!transactionMapping.ContainsKey(txId))
                                    {
                                        Console.WriteLine("Adding transaction mapping block {0} tx id {1}", block.blockNum, Transaction.txIdV8ToLegacy(txId));
                                        transactionMapping.Add(txId, block.blockNum);
                                    }
                                }
                                currentRequest.endpoint = broadcastGetBlock(block.blockNum, null, endpoint, 1, false);
                            }
                        }
                    }
                }
            }
        }

        public void processNewBlockHeight(ulong block_num, RemoteEndpoint endpoint)
        {
            Console.WriteLine("Received new block height {0}", block_num);

            lock (requestsQueue)
            {
                if (networkBlockHeight == 0)
                {
                    networkBlockHeight = block_num;
                    return;
                }

                for (int i = 0; i < (int)(block_num - networkBlockHeight); i++)
                {
                    if (requestsQueue.ContainsKey(networkBlockHeight + (ulong)i))
                    {
                        Console.WriteLine("Block {0} has already been queued for request, not queuing again", networkBlockHeight + (ulong)i);
                        continue;
                    }

                    requestsQueue.Add(networkBlockHeight + (ulong)i, new RequestData()
                    {
                        timeStamp = DateTime.Now,
                        blockNum = networkBlockHeight + (ulong)i,
                        endpoint = endpoint
                    });
                }
                networkBlockHeight = block_num;
            }
        }

        public static RemoteEndpoint broadcastGetBlock(ulong block_num, RemoteEndpoint skipEndpoint = null, RemoteEndpoint endpoint = null, int include_transactions = 0, bool full_header = false)
        {
            using (MemoryStream mw = new MemoryStream())
            {
                using (BinaryWriter writerw = new BinaryWriter(mw))
                {
                    writerw.WriteIxiVarInt(block_num);
                    writerw.Write((byte)include_transactions);
                    writerw.Write(full_header);

                    if (endpoint != null)
                    {
                        if (endpoint.isConnected())
                        {
                            endpoint.sendData(ProtocolMessageCode.getBlock3, mw.ToArray());
                            return endpoint;
                        }
                    }
                    return ProtocolMessage.broadcastProtocolMessageToSingleRandomNode(new char[] { 'M', 'H' }, ProtocolMessageCode.getBlock3, mw.ToArray(), block_num, skipEndpoint);
                }
            }
        }

        public void handleTransactionsChunk2(byte[] data, RemoteEndpoint endpoint)
        {
            lock (currentRequestLock)
            {
                if (currentRequest == null)
                {
                    return; // as long as there is no ongoing request, we don't care about txs
                }

                bool txsUpdated = false;
                bool txsForCurrentRequest = false;

                using (MemoryStream m = new MemoryStream(data))
                {
                    using (BinaryReader reader = new BinaryReader(m))
                    {
                        long msg_id = reader.ReadIxiVarInt();
                        int tx_count = (int)reader.ReadIxiVarUInt();

                        int max_tx_per_chunk = CoreConfig.maximumTransactionsPerChunk;
                        if (tx_count > max_tx_per_chunk)
                        {
                            tx_count = max_tx_per_chunk;
                        }

                        for (int i = 0; i < tx_count; i++)
                        {
                            if (m.Position == m.Length)
                            {
                                break;
                            }

                            int tx_len = (int)reader.ReadIxiVarUInt();
                            byte[] tx_bytes = reader.ReadBytes(tx_len);

                            Transaction tx = new Transaction(tx_bytes);

                            lock (transactionMapping)
                            {
                                if (!transactionMapping.ContainsKey(tx.id) || transactionMapping[tx.id] != currentRequest.blockNum)
                                {
                                    continue;
                                }

                                txsForCurrentRequest = true;

                                if (!currentRequest.transactions.Any(t => t.id.SequenceEqual(tx.id)))
                                {
                                    currentRequest.transactions.Add(tx);
                                    txsUpdated = true;
                                }
                            }
                        }
                    }
                }

                if(txsForCurrentRequest)
                {
                    currentRequest.timeStamp = DateTime.Now;
                }

                if (txsUpdated)
                {
                    if (currentRequest.block.transactions.All(txId => txId[0] == 0 || currentRequest.transactions.Any(t => t.id.SequenceEqual(txId)))) // ignore staking rewards
                    {
                        Console.WriteLine("All transactions have been received for block {0}, adding to repository", currentRequest.blockNum);
                        finalizeRequest();
                    }
                    else
                    {
                        Console.WriteLine("There are missing transactions for block {0} - {1} txs, still waiting", currentRequest.blockNum, currentRequest.block.transactions.Count - currentRequest.transactions.Count);
                    }
                }
            }
        }

        private void finalizeRequest()
        {
            lock (blockRepository)
            {
                lock (currentRequestLock)
                {
                    Block blk = currentRequest.block;
                    List<Transaction> txs = currentRequest.transactions.Where(tx => tx.type == (int)Transaction.Type.PoWSolution).ToList();

                    if (!blockRepository.ContainsKey(blk.blockNum))
                    {
                        blk.transactions.Clear(); // clear transaction data, is not needed anymore
                        blockRepository.Add(blk.blockNum, blk);
                        lock(activePoolBlockLock)
                        {
                            if(activePoolBlock != null && activePoolBlock.difficulty > blk.difficulty)
                            {
Console.WriteLine("Currently mined block has a higher difficulty than the last received one, switching to new block.");
                                activePoolBlock = null;
                            }
                        }
                    }

                    currentRequest = null;

                    foreach (Transaction tx in txs)
                    {
                        ulong minedBlockNum = 0;
                        // Extract the block number
                        using (MemoryStream m = new MemoryStream(tx.data))
                        {
                            using (BinaryReader reader = new BinaryReader(m))
                            {
                                minedBlockNum = reader.ReadUInt64();
                            }
                        }

                        Console.WriteLine("Found POW transaction, adding block {0} - miner {1} to known solved blocks list", minedBlockNum, (new Address(tx.pubKey)).ToString());

                        lock (knownSolvedBlocks)
                        {
                            if (knownSolvedBlocks.ContainsKey(minedBlockNum))
                            {
                                knownSolvedBlocks[minedBlockNum].Add(tx);
                            }
                            else
                            {
                                knownSolvedBlocks.Add(minedBlockNum, new List<Transaction>() { tx });
                            }
                        }

                        lock (activePoolBlockLock)
                        {
                            if (activePoolBlock != null && activePoolBlock.blockNum == minedBlockNum)
                            {
Console.WriteLine("Block that was currently mined has already been resolved by someone else, resetting active mining block");
                                activePoolBlock = null;
                            }
                        }
                    }
                }

                while (blockRepository.Count >= (long)ConsensusConfig.getRedactedWindowSize())
                {
                    ulong toRemove = blockRepository.Keys.Min();
                    lock (activePoolBlockLock)
                    {
                        if (activePoolBlock != null && activePoolBlock.blockNum == toRemove)
                        {
Console.WriteLine("Block that was currently mined is too old, resetting active mining block");
                            activePoolBlock = null;
                        }
                    }
                    blockRepository.Remove(toRemove);
                }
            }
        }

        public override Block getLastBlock()
        {
            lock(blockRepository)
            {
                if(blockRepository.Count > 0)
                {
                    return blockRepository[blockRepository.Keys.Max()];
                }
            }
            return null;
        }

        public override Wallet getWallet(byte[] id)
        {
            // TODO Properly implement this for multiple addresses
            if (balance.address != null && id.SequenceEqual(balance.address))
            {
                return new Wallet(balance.address, balance.balance);
            }
            return new Wallet(id, 0);
        }

        public override IxiNumber getWalletBalance(byte[] id)
        {
            // TODO Properly implement this for multiple addresses
            if (balance.address != null && id.SequenceEqual(balance.address))
            {
                return balance.balance;
            }
            return 0;
        }

        public override void shutdown()
        {
            IxianHandler.forceShutdown = true;
        }

        public override void parseProtocolMessage(ProtocolMessageCode code, byte[] data, RemoteEndpoint endpoint)
        {
            ProtocolMessage.parseProtocolMessage(this, code, data, endpoint);
        }

        public static void processPendingTransactions()
        {
            // TODO TODO improve to include failed transactions
            ulong last_block_height = IxianHandler.getLastBlockHeight();
            lock (PendingTransactions.pendingTransactions)
            {
                long cur_time = Clock.getTimestamp();
                List<PendingTransaction> tmp_pending_transactions = new List<PendingTransaction>(PendingTransactions.pendingTransactions);
                int idx = 0;
                foreach (var entry in tmp_pending_transactions)
                {
                    Transaction t = entry.transaction;
                    long tx_time = entry.addedTimestamp;

                    if (t.applied != 0)
                    {
                        PendingTransactions.pendingTransactions.RemoveAll(x => x.transaction.id.SequenceEqual(t.id));
                        continue;
                    }

                    // if transaction expired, remove it from pending transactions
                    if (last_block_height > ConsensusConfig.getRedactedWindowSize() && t.blockHeight < last_block_height - ConsensusConfig.getRedactedWindowSize())
                    {
                        Console.WriteLine("Error sending the transaction {0}", Transaction.txIdV8ToLegacy(t.id));
                        PendingTransactions.pendingTransactions.RemoveAll(x => x.transaction.id.SequenceEqual(t.id));
                        continue;
                    }

                    if (cur_time - tx_time > 40) // if the transaction is pending for over 40 seconds, resend
                    {
                        CoreProtocolMessage.broadcastProtocolMessage(new char[] { 'M', 'H' }, ProtocolMessageCode.transactionData, t.getBytes(), null);
                        entry.addedTimestamp = cur_time;
                        entry.confirmedNodeList.Clear();
                    }

                    if (entry.confirmedNodeList.Count() > 3) // already received 3+ feedback
                    {
                        continue;
                    }

                    if (cur_time - tx_time > 20) // if the transaction is pending for over 20 seconds, send inquiry
                    {
                        CoreProtocolMessage.broadcastGetTransaction(t.id, 0);
                    }

                    idx++;
                }
            }
        }

        public override BlockHeader getBlockHeader(ulong blockNum)
        {
            return BlockHeaderStorage.getBlockHeader(blockNum);
        }
        
        public void handleInventory2(byte[] data, RemoteEndpoint endpoint)
        {
            using (MemoryStream m = new MemoryStream(data))
            {
                using (BinaryReader reader = new BinaryReader(m))
                {
                    ulong item_count = reader.ReadIxiVarUInt();
                    if (item_count > (ulong)CoreConfig.maxInventoryItems)
                    {
                        Logging.warn("Received {0} inventory items, max items is {1}", item_count, CoreConfig.maxInventoryItems);
                        item_count = (ulong)CoreConfig.maxInventoryItems;
                    }

                    for (ulong i = 0; i < item_count; i++)
                    {
                        ulong len = reader.ReadIxiVarUInt();
                        byte[] item_bytes = reader.ReadBytes((int)len);
                        InventoryItem item = InventoryCache.decodeInventoryItem(item_bytes);
                        switch (item.type)
                        {
                            case InventoryItemTypes.blockSignature:
                                var iis = (InventoryItemSignature)item;
                                if (iis.blockNum > endpoint.blockHeight)
                                {
                                    endpoint.blockHeight = iis.blockNum;
                                }
                                break;

                            case InventoryItemTypes.block:
                                var iib = ((InventoryItemBlock)item);
                                if (iib.blockNum > endpoint.blockHeight)
                                {
                                    endpoint.blockHeight = iib.blockNum;
                                }
                                break;
                        }
                    }

                    if (networkBlockHeight < endpoint.blockHeight)
                    {
                        processNewBlockHeight(endpoint.blockHeight, endpoint);
                    }
                }
            }
        }
        
        [ThreadStatic] private static byte[] dummyExpandedNonce = null;
        [ThreadStatic] private static int lastNonceLength = 0;
        private static byte[] expandNonce(byte[] nonce, int expand_length)
        {
            if (dummyExpandedNonce == null)
            {
                dummyExpandedNonce = new byte[expand_length];
                for (int i = 0; i < dummyExpandedNonce.Length; i++)
                {
                    dummyExpandedNonce[i] = 0x23;
                }
            }

            // set dummy with nonce
            for (int i = 0; i < nonce.Length; i++)
            {
                dummyExpandedNonce[i] = nonce[i];
            }

            // clear any bytes from last nonce
            for(int i = nonce.Length; i < lastNonceLength; i++)
            {
                dummyExpandedNonce[i] = 0x23;
            }

            lastNonceLength = nonce.Length;

            return dummyExpandedNonce;
        }

        private static byte[] findHash_v2(byte[] data, byte[] salt)
        {
            try
            {
                byte[] hash = new byte[32];
                IntPtr data_ptr = Marshal.AllocHGlobal(data.Length);
                IntPtr salt_ptr = Marshal.AllocHGlobal(salt.Length);
                Marshal.Copy(data, 0, data_ptr, data.Length);
                Marshal.Copy(salt, 0, salt_ptr, salt.Length);
                UIntPtr data_len = (UIntPtr)data.Length;
                UIntPtr salt_len = (UIntPtr)salt.Length;
                IntPtr result_ptr = Marshal.AllocHGlobal(32);
                int result = NativeMethods.argon2id_hash_raw((UInt32)2, (UInt32)2048, (UInt32)2, data_ptr, data_len, salt_ptr, salt_len, result_ptr, (UIntPtr)32);
                Marshal.Copy(result_ptr, hash, 0, 32);
                Marshal.FreeHGlobal(data_ptr);
                Marshal.FreeHGlobal(result_ptr);
                Marshal.FreeHGlobal(salt_ptr);
                return hash;
            }
            catch (Exception e)
            {
                Logging.error(string.Format("Error during mining: {0}", e.Message));
                return null;
            }
        }
        
        public static byte[] getHashCeilFromDifficulty(ulong difficulty)
        {
            /*
             * difficulty is an 8-byte number from 0 to 2^64-1, which represents how hard it is to find a hash for a certain block
             * the dificulty is converted into a 'ceiling value', which specifies the maximum value a hash can have to be considered valid under that difficulty
             * to do this, follow the attached algorithm:
             *  1. calculate a bit-inverse value of the difficulty
             *  2. create a comparison byte array with the ceiling value of length 10 bytes
             *  3. set the first two bytes to zero
             *  4. insert the inverse difficulty as the next 8 bytes (mind the byte order!)
             *  5. the remaining 22 bytes are assumed to be 'FF'
             */
            byte[] hash_ceil = new byte[10];
            hash_ceil[0] = 0x00;
            hash_ceil[1] = 0x00;
            for(int i=0;i<8;i++)
            {
                int shift = 8 * (7 - i);
                ulong mask = ((ulong)0xff) << shift;
                byte cb = (byte)((difficulty & mask) >> shift);
                hash_ceil[i + 2] = (byte)~cb;
            }
            return hash_ceil;
        }

        public Block getBlock(ulong blocknum)
        {
            Block requested = null;
            lock (blockRepository)
            {
                if (blockRepository.ContainsKey(blocknum))
                {
                    requested = new Block(blockRepository[blocknum]);
                }
            }

            return requested;
        }

        private bool validateHash_v2(byte[] hash, byte[] hash_ceil)
        {
            if (hash == null || hash.Length < 32)
            {
                return false;
            }
            for (int i = 0; i < hash.Length; i++)
            {
                byte cb = i < hash_ceil.Length ? hash_ceil[i] : (byte)0xff;
                if (cb > hash[i]) return true;
                if (cb < hash[i]) return false;
            }
            // if we reach this point, the hash is exactly equal to the ceiling we consider this a 'passing hash'
            return true;
        }
        
        public bool verifyNonce_v3(string nonce, ulong blocknum, byte[] solverAddress, ulong difficulty)
        {
            if (nonce == null || nonce.Length < 1 || nonce.Length > 128)
            {
                return false;
            }

            Block block = getBlock(blocknum);
            if (block == null)
                return false;

            // TODO protect against spamming with invalid nonce/block_num
            byte[] p1 = new byte[block.blockChecksum.Length + solverAddress.Length];
            System.Buffer.BlockCopy(block.blockChecksum, 0, p1, 0, block.blockChecksum.Length);
            System.Buffer.BlockCopy(solverAddress, 0, p1, block.blockChecksum.Length, solverAddress.Length);

            byte[] nonce_bytes = Crypto.stringToHash(nonce);
            byte[] fullnonce = expandNonce(nonce_bytes, 234236);
            byte[] hash = findHash_v2(p1, fullnonce);

            if (validateHash_v2(hash, getHashCeilFromDifficulty(difficulty)) == true)
            {
                // Hash is valid
                return true;
            }

            return false;
        }

        public bool sendSolution(byte[] nonce, ulong blocknum)
        {
            WalletStorage ws = IxianHandler.getWalletStorage();
            byte[] from = ws.getPrimaryAddress();
            byte[] pubkey = ws.getPrimaryPublicKey();
            byte[] data = null;

            using (MemoryStream mw = new MemoryStream())
            {
                using (BinaryWriter writerw = new BinaryWriter(mw))
                {
                    writerw.Write(blocknum);
                    writerw.Write(Crypto.hashToString(nonce));
                    data = mw.ToArray();
                }
            }

            lock (activePoolBlockLock)
            {
                lock (knownSolvedBlocks)
                {
                    knownSolvedBlocks.Add(activePoolBlock.blockNum, new List<Transaction>());
                }

                activePoolBlock = null;
            }

            Transaction transaction = new Transaction((int) Transaction.Type.PoWSolution, new IxiNumber(0),
                new IxiNumber(0), ConsensusConfig.ixianInfiniMineAddress, from, data, pubkey,
                getHighestKnownNetworkBlockHeight());

            if (IxianHandler.addTransaction(transaction, true))
            {
                Console.WriteLine("Sending transaction, txid: {0}\n", Transaction.txIdV8ToLegacy(transaction.id));
                return true;
            }
            else
            {
                Console.WriteLine("Could not send transaction\n");
                return false;
            }
        }

        public Block getMiningBlock()
        {            
            lock (activePoolBlockLock)
            {
                if (activePoolBlock == null)
                {
                    activePoolBlock = blockRepository.Where(x => !knownSolvedBlocks.ContainsKey(x.Key)).Select(x => x.Value).OrderBy(x => x.difficulty).FirstOrDefault();
                }
                return activePoolBlock;
            }
        }
    }
}
