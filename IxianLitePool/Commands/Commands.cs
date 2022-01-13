using IXICore;
using IXICore.Meta;
using IXICore.Network;
using IXICore.Utils;
using LP.DB;
using LP.Meta;
using LP.Network;
using LP.Pool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace IxianLitePool
{
    class Commands
    {
        Node node = null;
        public Commands(Node node)
        {
            this.node = node;
        }

        public void handleCommand(string line)
        {
            if(string.IsNullOrEmpty(line))
            {
                return;
            }

            line = line.Trim();
            int ws_index = line.IndexOf(' ');
            if(ws_index == -1)
            {
                ws_index = line.Length;
            }
            string command = line.Substring(0, ws_index).ToLower();
            switch(command)
            {
                case "exit":
                case "quit":
                    Program.stop();
                    break;

                case "help":
                    handleHelp();
                    break;

                case "status":
                    handleStatus();
                    break;

                case "balance":
                    handleBalance();
                    break;

                case "address":
                    handleAddress();
                    break;

                case "addresses":
                    handleAddresses();
                    break;

                case "backup":
                    handleBackup();
                    break;

                case "changepass":
                    handleChangePass();
                    break;

                case "send":
                    handleSend(line);
                    break;

                case "verify":
                    handleVerify(line);
                    break;

                case "block":
                    handleBlock(line);
                    break;

                case "getblock":
                    handleGetBlock(line);
                    break;

                case "lockapi":
                    APIServer.Instance.lockServer() ;
                    Console.WriteLine("API server in lockdown mode.");
                    break;

                case "unlockapi":
                    APIServer.Instance.unlockServer();
                    Console.WriteLine("API server online.");
                    break;

                case "cleanupdb":
                    handleCleanUpDB();
                    break;

                case "pausesync":
                    node.pauseSync();
                    Console.WriteLine("Sync process paused.");
                    break;

                case "resumesync":
                    node.resumeSync();
                    Console.WriteLine("Sync process resumed.");
                    break;

                case "pausepay":
                    Payment.Instance.pause();
                    Console.WriteLine("Payment process paused.");
                    break;

                case "resumepay":
                    Payment.Instance.resume();
                    Console.WriteLine("Payment process resumed.");
                    break;

                case "addnote":
                    handleAddNote(line);
                    break;

                case "enablenote":
                    handleEnableNote(line);
                    break;

                case "disablenote":
                    handleDisableNote(line);
                    break;
            }
        }

        private void handleCleanUpDB()
        {
            Console.WriteLine("Removing old blocks from storage...");
            BlockStorage.Instance.cleanUpBlocks(IxianHandler.getHighestKnownNetworkBlockHeight() - ConsensusConfig.getRedactedWindowSize(Block.maxVersion));
            Console.WriteLine("Removing old shares from storage...");
            PoolDB.Instance.cleanUpShares();
            Console.WriteLine("Cleanup complete.");
        }

        private void handleGetBlock(string line)
        {
            string[] split = line.Split(new string[] { " " }, StringSplitOptions.None);
            if (split.Count() < 2)
            {
                Console.WriteLine("Incorrect parameters for getblock. Should be block number.\n");
                return;
            }

            ulong blockNum = 0;
            if (!ulong.TryParse(split[1], out blockNum))
            {
                Console.WriteLine("Incorrect parameters for getblock. BlockNum is not a number.\n");
                return;
            }

            Console.WriteLine("Requesting block number {0} from network", blockNum);

            node.requestBlock(blockNum);
        }

        void handleBlock(string line)
        {
            string[] split = line.Split(new string[] { " " }, StringSplitOptions.None);
            if (split.Count() < 2)
            {
                Console.WriteLine("Incorrect parameters for block. Should be block number.\n");
                return;
            }

            ulong blockNum = 0;
            if (!ulong.TryParse(split[1], out blockNum))
            {
                Console.WriteLine("Incorrect parameters for block. BlockNum is not a number.\n");
                return;
            }

            RepositoryBlock blk = node.getBlock(blockNum);
            if(blk == null)
            {
                Console.WriteLine("Incorrect parameters for block. BlockNum has not been retrieved or doesn't exist.\n");
                return;
            }

            Console.WriteLine("Block Number: {0}", blk.blockNum);
            Console.WriteLine("Block Version: {0}", blk.version);
            Console.WriteLine("Block Difficulty: {0}", blk.difficulty);
            Console.WriteLine("Block Checksum: {0}", Convert.ToBase64String(blk.blockChecksum));
        }


        void handleHelp()
        {
            Console.WriteLine("Ixian LitePool usage:");
            Console.WriteLine("\texit\t\t\t-exits the litewallet");
            Console.WriteLine("\thelp\t\t\t-shows this help message");
            Console.WriteLine("\tstatus\t\t\t-shows the number of connected DLT nodes");
            Console.WriteLine("\tbalance\t\t\t-shows this wallet balance");
            Console.WriteLine("\taddress\t\t\t-shows this wallet's primary address");
            Console.WriteLine("\taddresses\t\t-shows all addresses for this wallet");
            Console.WriteLine("\tbackup\t\t\t-backup this wallet as an IXIHEX text");
            Console.WriteLine("\tchangepass\t\t-changes this wallet's password");
            //Console.WriteLine("\tverify [txid]\t\t-verifies the specified transaction txid");
            Console.WriteLine("\tsend [address] [amount]\t-sends IxiCash to the specified address");
            // generate new address, view all address balances
            // change password
            Console.WriteLine("");
        }

        void handleBalance()
        {
            var balance = node.getBalance();
            string verified = "";
            if (balance.verified)
            {
                //verified = " (verified)"; // not yet
            }
            Console.WriteLine("Balance: {0} IXI{1}\n", balance.balance, verified);
        }

        void handleAddress()
        {
            Console.WriteLine("Primary address: {0}\n", Base58Check.Base58CheckEncoding.EncodePlain(IxianHandler.getWalletStorage().getPrimaryAddress()));
        }

        void handleAddresses()
        {
            List<Address> address_list = IxianHandler.getWalletStorage().getMyAddresses();

            foreach (Address addr in address_list)
            {
                Console.WriteLine("{0}", addr.ToString());
            }
            Console.WriteLine("");
        }

        void handleBackup()
        {
            List<byte> wallet = new List<byte>();
            wallet.AddRange(IxianHandler.getWalletStorage().getRawWallet());
            Console.WriteLine("IXIHEX" + Crypto.hashToString(wallet.ToArray()));
            Console.WriteLine("");
        }

        void handleChangePass()
        {
            // Request the current wallet password
            bool success = false;
            while (!success)
            {
                string password = "";
                if (password.Length < 10)
                {
                    Logging.flush();
                    Console.Write("Enter your current wallet password: ");
                    password = ConsoleHelpers.getPasswordInput();
                }
                if (password.Length == 0)
                {
                    break;
                }

                // Read the wallet using the provided password
                if (IxianHandler.getWalletStorage().isValidPassword(password))
                {
                    success = true;
                }
            }

            if (success == false)
                return;

            // Request a new password
            string new_password = "";
            while (new_password.Length < 10)
            {
                new_password = ConsoleHelpers.requestNewPassword("Enter a new password for your wallet: ");
                if (new_password.Length == 0)
                {
                    continue;
                }
            }
            if (IxianHandler.getWalletStorage().writeWallet(new_password))
                Console.WriteLine("Wallet password changed.");
        }

        void handleSend(string line)
        {
            string[] split = line.Split(new string[] { " " }, StringSplitOptions.None);
            if (split.Count() < 3)
            {
                Console.WriteLine("Incorrect parameters for send. Should be address and amount.\n");
                return;
            }
            string address = split[1];
            // Validate the address first
            byte[] _address = Base58Check.Base58CheckEncoding.DecodePlain(address);
            if (Address.validateChecksum(_address) == false)
            {
                Console.WriteLine("Invalid address checksum!. Please make sure you typed the address correctly.\n");
                return;
            }
            // Make sure the amount is positive
            IxiNumber amount = new IxiNumber(split[2]);
            if (amount < (long)0)
            {
                Console.WriteLine("Please type a positive amount.\n");
                return;
            }
            node.sendTransaction(address, amount);
        }

        void handleVerify(string line)
        {
            string[] split = line.Split(new string[] { " " }, StringSplitOptions.None);
            if (split.Count() < 2)
            {
                Console.WriteLine("Incorrect parameters for verify. Should be at least the txid.\n");
                return;
            }

            string txid = split[1];

            int connectionsOut = NetworkClientManager.getConnectedClients(true).Count();
            if (connectionsOut < 3)
            {
                Console.WriteLine("Need at least 3 node connections to verify transactions.");
                return;
            }

            Console.WriteLine("Posting Transaction Inclusion Verification request for {0}", txid);

            // TODO
            //tiv.verifyTransactionInclusion(txid);
        }

        void handleStatus()
        {
            Console.WriteLine("Last Block Height: {0}", IxianHandler.getLastBlockHeight());
            Console.WriteLine("Network Block Height: {0}", IxianHandler.getHighestKnownNetworkBlockHeight());

            int connectionsOut = NetworkClientManager.getConnectedClients(true).Count();
            Console.WriteLine("Connections: {0}", connectionsOut);

            Console.WriteLine("Pending transactions: {0}\n", PendingTransactions.pendingTransactionCount());
        }

        private void handleAddNote(string line)
        {
            string notificationData = line.Substring(7).Trim();

            string[] split = notificationData.Split(new string[] { " " }, StringSplitOptions.None);

            if(split.Count() < 2)
            {
                Console.WriteLine("Incorrect parameters for addnote. Should be notification type (primary, info, success, warning, danger) followed by content.\n");
                return;
            }

            NotificationType type;
            string content;

            switch(split[0].Trim().ToLower())
            {
                case "primary":
                    type = NotificationType.Primary;
                    content = notificationData.Substring(7).Trim();
                    break;
                case "info":
                    type = NotificationType.Info;
                    content = notificationData.Substring(4).Trim();
                    break;
                case "success":
                    type = NotificationType.Success;
                    content = notificationData.Substring(7).Trim();
                    break;
                case "warning":
                    type = NotificationType.Warning;
                    content = notificationData.Substring(7).Trim();
                    break;
                case "danger":
                    type = NotificationType.Danger;
                    content = notificationData.Substring(6).Trim();
                    break;
                default:
                    Console.WriteLine("Incorrect parameters for addnote. Should be notification type (primary, info, success, warning, danger) followed by content.\n");
                    return;
            }

            if (string.IsNullOrEmpty(content))
            {
                Console.WriteLine("Incorrect parameters for addnote. Should be notification type (primary, info, success, warning, danger) followed by content.\n");
                return;
            }

            int id = Pool.Instance.addNotification(type, content, false);

            if(id > 0)
            {
                Console.WriteLine("Successfully created notification with ID {0}, use enablenote command to activate: type - {1}   content - {2}", id, type.ToString(), content);
            }
        }

        private void handleEnableNote(string line)
        {
            string[] split = line.Split(new string[] { " " }, StringSplitOptions.None);
            if (split.Count() < 2)
            {
                Console.WriteLine("Incorrect parameters for enablenote. Should be notification id.\n");
                return;
            }

            int noteId = 0;
            if (!int.TryParse(split[1], out noteId))
            {
                Console.WriteLine("Incorrect parameters for enablenote. Should be notification id.\n");
                return;
            }

            Console.WriteLine("Enabling notification with id {0}", noteId);

            Pool.Instance.updateNotificationStatus(noteId, true);
        }

        private void handleDisableNote(string line)
        {
            string[] split = line.Split(new string[] { " " }, StringSplitOptions.None);
            if (split.Count() < 2)
            {
                Console.WriteLine("Incorrect parameters for disablenote. Should be note id.\n");
                return;
            }

            int noteId = 0;
            if (!int.TryParse(split[1], out noteId))
            {
                Console.WriteLine("Incorrect parameters for disablenote. Should be note id.\n");
                return;
            }

            Console.WriteLine("Disabling notification with id {0}", noteId);

            Pool.Instance.updateNotificationStatus(noteId, false);
        }

    }
}
