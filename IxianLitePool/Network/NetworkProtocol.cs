using IxianLitePool;
using IXICore;
using IXICore.Meta;
using IXICore.Network;
using IXICore.Utils;
using LP.Meta;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;

namespace LP.Network
{
    public class ProtocolMessage
    {
        public static ProtocolMessageCode waitingFor = 0;
        public static bool blocked = false;

        public static void setWaitFor(ProtocolMessageCode value)
        {
            waitingFor = value;
            blocked = true;
        }

        public static void wait(int timeout_seconds)
        {
            DateTime start = DateTime.UtcNow;
            while(blocked)
            {
                if((DateTime.UtcNow - start).TotalSeconds > timeout_seconds)
                {
                    Logging.warn("Timeout occured while waiting for " + waitingFor);
                    break;
                }
                Thread.Sleep(250);
            }
        }

        // Unified protocol message parsing
        public static void parseProtocolMessage(Node node, ProtocolMessageCode code, byte[] data, RemoteEndpoint endpoint)
        {
            if (endpoint == null)
            {
                Logging.error("Endpoint was null. parseProtocolMessage");
                return;
            }
            try
            {
                switch (code)
                {
                    case ProtocolMessageCode.hello:
                        using (MemoryStream m = new MemoryStream(data))
                        {
                            using (BinaryReader reader = new BinaryReader(m))
                            {
                                CoreProtocolMessage.processHelloMessageV6(endpoint, reader);
                            }
                        }
                        break;


                    case ProtocolMessageCode.helloData:
                        using (MemoryStream m = new MemoryStream(data))
                        {
                            using (BinaryReader reader = new BinaryReader(m))
                            {
                                if (CoreProtocolMessage.processHelloMessageV6(endpoint, reader))
                                {
                                    char node_type = endpoint.presenceAddress.type;
                                    if (node_type != 'M' && node_type != 'H')
                                    {
                                        CoreProtocolMessage.sendBye(endpoint, ProtocolByeCode.expectingMaster, string.Format("Expecting master node."), "", true);
                                        return;
                                    }

                                    ulong last_block_num = reader.ReadIxiVarUInt();

                                    int bcLen = (int)reader.ReadIxiVarUInt();
                                    byte[] block_checksum = reader.ReadBytes(bcLen);

                                    endpoint.blockHeight = last_block_num;

                                    int block_version = (int)reader.ReadIxiVarUInt();

                                    // Process the hello data
                                    endpoint.helloReceived = true;
                                    NetworkClientManager.recalculateLocalTimeDifference();

                                    if (endpoint.presenceAddress.type == 'M' || endpoint.presenceAddress.type == 'H')
                                    {
                                        node.setNetworkBlock(last_block_num, endpoint);

                                        // Get random presences
                                        endpoint.sendData(ProtocolMessageCode.getRandomPresences, new byte[1] { (byte)'M' });
                                        endpoint.sendData(ProtocolMessageCode.getRandomPresences, new byte[1] { (byte)'H' });

                                        CoreProtocolMessage.subscribeToEvents(endpoint);
                                    }
                                }
                            }
                        }
                        break;

                    case ProtocolMessageCode.balance:
                        {
                            using (MemoryStream m = new MemoryStream(data))
                            {
                                using (BinaryReader reader = new BinaryReader(m))
                                {
                                    int address_length = reader.ReadInt32();
                                    byte[] address = reader.ReadBytes(address_length);

                                    // Retrieve the latest balance
                                    IxiNumber balance = new IxiNumber(reader.ReadString());

                                    if (address.SequenceEqual(IxianHandler.getWalletStorage().getPrimaryAddress()))
                                    {
                                        // Retrieve the blockheight for the balance
                                        ulong block_height = reader.ReadUInt64();

                                        if (block_height > Node.balance.blockHeight && (Node.balance.balance != balance || Node.balance.blockHeight == 0))
                                        {
                                            byte[] block_checksum = reader.ReadBytes(reader.ReadInt32());

                                            Node.balance.address = address;
                                            Node.balance.balance = balance;
                                            Node.balance.blockHeight = block_height;
                                            Node.balance.blockChecksum = block_checksum;
                                            Node.balance.verified = false;
                                        }
                                    }
                                }
                            }
                        }
                        break;

                    case ProtocolMessageCode.balance2:
                        {
                            using (MemoryStream m = new MemoryStream(data))
                            {
                                using (BinaryReader reader = new BinaryReader(m))
                                {
                                    int address_length = (int)reader.ReadIxiVarUInt();
                                    byte[] address = reader.ReadBytes(address_length);

                                    int balance_bytes_len = (int)reader.ReadIxiVarUInt();
                                    byte[] balance_bytes = reader.ReadBytes(balance_bytes_len);

                                    // Retrieve the latest balance
                                    IxiNumber balance = new IxiNumber(new BigInteger(balance_bytes));

                                    if (address.SequenceEqual(IxianHandler.getWalletStorage().getPrimaryAddress()))
                                    {
                                        // Retrieve the blockheight for the balance
                                        ulong block_height = reader.ReadIxiVarUInt();

                                        if (block_height > Node.balance.blockHeight && (Node.balance.balance != balance || Node.balance.blockHeight == 0))
                                        {
                                            byte[] block_checksum = reader.ReadBytes((int)reader.ReadIxiVarUInt());

                                            Node.balance.address = address;
                                            Node.balance.balance = balance;
                                            Node.balance.blockHeight = block_height;
                                            Node.balance.blockChecksum = block_checksum;
                                            Node.balance.verified = false;
                                        }
                                    }
                                }
                            }
                        }
                        break;

                    case ProtocolMessageCode.updatePresence:
                        {
                            // Parse the data and update entries in the presence list
                            PresenceList.updateFromBytes(data, 0);
                        }
                        break;

                    case ProtocolMessageCode.blockHeaders2:
                        {
 //                           // Forward the block headers to the TIV handler
                            Node.tiv.receivedBlockHeaders2(data, endpoint);
                        }
                        break;

                    case ProtocolMessageCode.pitData2:
                        {
                            Node.tiv.receivedPIT2(data, endpoint);
                        }
                        break;

                    case ProtocolMessageCode.transactionData:
                        {
                            Transaction tx = new Transaction(data, true);

                            if (endpoint.presenceAddress.type == 'M' || endpoint.presenceAddress.type == 'H')
                            {
                                PendingTransactions.increaseReceivedCount(tx.id, endpoint.presence.wallet);
                            }

                            if(Node.tiv.receivedNewTransaction(tx))
                            {
                                Console.WriteLine("Received new transaction {0}", Transaction.txIdV8ToLegacy(tx.id));
                            }
                        }
                        break;

                    case ProtocolMessageCode.inventory2:
                        node.handleInventory2(data, endpoint);
                        break;

                    case ProtocolMessageCode.bye:
                        CoreProtocolMessage.processBye(data, endpoint);
                        break;

                    case ProtocolMessageCode.blockData:
                        node.handleBlockData(data, endpoint);
                        break;
                    
                    case ProtocolMessageCode.transactionsChunk2:
                        node.handleTransactionsChunk2(data, endpoint);
                        break;

                    case ProtocolMessageCode.blockHeight:
                        node.handleBlockHeight(data, endpoint);
                        break;

                    default:
                        break;

                }
            }
            catch (Exception e)
            {
                Logging.error("Error parsing network message. Details: {0}", e);
            }

            if(waitingFor == code)
            {
                blocked = false;
            }
        }
        
        public static RemoteEndpoint broadcastProtocolMessageToSingleRandomNode(char[] types, ProtocolMessageCode code, byte[] data, ulong block_num, RemoteEndpoint skipEndpoint = null)
        {
            if (data == null)
            {
                Logging.warn(string.Format("Invalid protocol message data for {0}", code));
                return null;
            }

            lock (NetworkClientManager.networkClients)
            {
                lock (NetworkServer.connectedClients)
                {
                    int serverCount = 0;
                    int clientCount = 0;
                    List<NetworkClient> servers = null;
                    List<RemoteEndpoint> clients = null;

                    if (types == null)
                    {
                        servers = NetworkClientManager.networkClients.FindAll(x => x.blockHeight > block_num && x.isConnected() && x.helloReceived);
                        clients = NetworkServer.connectedClients.FindAll(x => x.blockHeight > block_num && x.isConnected() && x.helloReceived);

                        serverCount = servers.Count();
                        clientCount = clients.Count();

                        if (serverCount == 0 && clientCount == 0)
                        {
                            servers = NetworkClientManager.networkClients.FindAll(x => x.blockHeight == block_num && x.isConnected() && x.helloReceived);
                            clients = NetworkServer.connectedClients.FindAll(x => x.blockHeight == block_num && x.isConnected() && x.helloReceived);
                        }
                    }
                    else
                    {
                        servers = NetworkClientManager.networkClients.FindAll(x => x.blockHeight > block_num && x.presenceAddress != null && types.Contains(x.presenceAddress.type) && x.isConnected() && x.helloReceived);
                        clients = NetworkServer.connectedClients.FindAll(x => x.blockHeight > block_num && x.presenceAddress != null && types.Contains(x.presenceAddress.type) && x.isConnected() && x.helloReceived);

                        serverCount = servers.Count();
                        clientCount = clients.Count();

                        if (serverCount == 0 && clientCount == 0)
                        {
                            servers = NetworkClientManager.networkClients.FindAll(x => x.blockHeight == block_num && x.presenceAddress != null && types.Contains(x.presenceAddress.type) && x.isConnected() && x.helloReceived);
                            clients = NetworkServer.connectedClients.FindAll(x => x.blockHeight == block_num && x.presenceAddress != null && types.Contains(x.presenceAddress.type) && x.isConnected() && x.helloReceived);
                        }
                    }

                    serverCount = servers.Count();
                    clientCount = clients.Count();

                    if (serverCount == 0 && clientCount == 0)
                    {
                        return null;
                    }

                    Random r = new Random();
                    int rIdx = r.Next(serverCount + clientCount);

                    RemoteEndpoint re = null;

                    if (rIdx < serverCount)
                    {
                        re = servers[rIdx];
                    }
                    else
                    {
                        re = clients[rIdx - serverCount];
                    }

                    if (re == skipEndpoint && serverCount + clientCount > 1)
                    {
                        if (rIdx + 1 < serverCount)
                        {
                            re = servers[rIdx + 1];
                        }
                        else if (rIdx + 1 < serverCount + clientCount)
                        {
                            re = clients[rIdx + 1 - serverCount];
                        }
                        else if (serverCount > 0)
                        {
                            re = servers[0];
                        }
                        else if (clientCount > 0)
                        {
                            re = clients[0];
                        }
                    }

                    if (re != null && re.isConnected())
                    {
                        re.sendData(code, data);
                        return re;
                    }
                    return null;
                }
            }
        }

    }
}
