using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fclp;
using IXICore.Meta;
using IXICore.Network;

namespace LP.Meta
{
    class Config
    {
        public static string walletFile = "ixian.wal";
        public static readonly string version = "xlp-0.0.1";

        public static int serverPort = 10234;

        public static int apiPort = 8081;

        public static Dictionary<string, string> apiUsers = new Dictionary<string, string>();
        public static List<string> apiAllowedIps = new List<string>();
        public static List<string> apiBinds = new List<string>();

        public static int maxLogSize = 50; // MB
        public static int maxLogCount = 10;

        public static int logVerbosity = (int) LogSeverity.info + (int) LogSeverity.warn + (int) LogSeverity.error;

        public static string externalIp = "";

        public static int maxOutgoingConnections = 12;

        public static string configFilename = "ixian.cfg";

        private Config()
        {

        }

        private static string outputHelp()
        {
            Console.WriteLine("Starts a new instance of Ixian Lite Pool");
            Console.WriteLine("");
            Console.WriteLine(
                " IxianLitePool.exe [-h] [-v] [-p 10234] [-a 8081] [-i ip] [-w ixian.wal] [-n seed1.ixian.io:10234]");
            Console.WriteLine(
                "   [--config ixian.cfg] [--maxLogSize 50] [--maxLogCount 10] [--logVerbosity 14] [--walletPassword]");
            Console.WriteLine("   [--maxOutgoingConnections]");
            Console.WriteLine("");
            Console.WriteLine("    -h\t\t\t Displays this help");
            Console.WriteLine("    -v\t\t\t Displays version");
            Console.WriteLine("    -p\t\t\t Port to listen on");
            Console.WriteLine("    -a\t\t\t HTTP/API port to listen on");
            Console.WriteLine("    -i\t\t\t External IP Address to use");
            Console.WriteLine("    -w\t\t\t Specify location of the ixian.wal file");
            Console.WriteLine("    -n\t\t\t Specify which seed node to use");
            Console.WriteLine("    --config\t\t Specify config filename (default ixian.cfg)");
            Console.WriteLine("    --maxLogSize\t Specify maximum log file size in MB");
            Console.WriteLine("    --maxLogCount\t Specify maximum number of log files");
            Console.WriteLine(
                "    --logVerbosity\t Sets log verbosity (0 = none, trace = 1, info = 2, warn = 4, error = 8)");
            Console.WriteLine("    --walletPassword\t Specify the password for the wallet (be careful with this)");
            Console.WriteLine("    --verboseOutput\t Starts node with verbose output.");
            Console.WriteLine("    --maxOutgoingConnections\t Max outgoing connections.");
            Console.WriteLine("----------- Config File Options -----------");
            Console.WriteLine(" Config file options should use parameterName = parameterValue syntax.");
            Console.WriteLine(" Each option should be specified in its own line. Example:");
            Console.WriteLine("    dltPort = 10234");
            Console.WriteLine("    apiPort = 8081");
            Console.WriteLine("");
            Console.WriteLine(" Available options:");
            Console.WriteLine("    dltPort\t\t Port to listen on (same as -p CLI)");
            Console.WriteLine("    apiPort\t\t HTTP/API port to listen on (same as -a CLI)");
            Console.WriteLine(
                "    apiAllowIp\t\t Allow API connections from specified source or sources (can be used multiple times)");
            Console.WriteLine(
                "    apiBind\t\t Bind to given address to listen for API connections (can be used multiple times)");
            Console.WriteLine(
                "    addApiUser\t\t Adds user:password that can access the API (can be used multiple times)");

            Console.WriteLine("    externalIp\t\t External IP Address to use (same as -i CLI)");
            Console.WriteLine(
                "    addPeer\t\t Specify which seed node to use (same as -n CLI) (can be used multiple times)");
            Console.WriteLine("    maxLogSize\t\t Specify maximum log file size in MB (same as --maxLogSize CLI)");
            Console.WriteLine("    maxLogCount\t\t Specify maximum number of log files (same as --maxLogCount CLI)");
            Console.WriteLine("    logVerbosity\t Sets log verbosity (same as --logVerbosity CLI)");

            return "";
        }

        private static void readConfigFile(string filename)
        {
            if (!File.Exists(filename))
            {
                return;
            }

            Logging.info("Reading config file: " + filename);
            List<string> lines = File.ReadAllLines(filename).ToList();
            foreach (string line in lines)
            {
                string[] option = line.Split('=');
                if (option.Length < 2)
                {
                    continue;
                }

                string key = option[0].Trim(new char[] {' ', '\t', '\r', '\n'});
                string value = option[1].Trim(new char[] {' ', '\t', '\r', '\n'});

                if (key.StartsWith(";"))
                {
                    continue;
                }

                Logging.info("Processing config parameter '" + key + "' = '" + value + "'");
                switch (key)
                {
                    case "dltPort":
                        serverPort = int.Parse(value);
                        break;
                    case "apiPort":
                        apiPort = int.Parse(value);
                        break;
                    case "apiAllowIp":
                        apiAllowedIps.Add(value);
                        break;
                    case "apiBind":
                        apiBinds.Add(value);
                        break;
                    case "addApiUser":
                        string[] credential = value.Split(':');
                        if (credential.Length == 2)
                        {
                            apiUsers.Add(credential[0], credential[1]);
                        }

                        break;
                    case "externalIp":
                        externalIp = value;
                        break;
                    case "addPeer":
                        CoreNetworkUtils.seedNodes.Add(new string[2] {value, null});
                        break;
                    case "maxLogSize":
                        maxLogSize = int.Parse(value);
                        break;
                    case "maxLogCount":
                        maxLogCount = int.Parse(value);
                        break;
                    case "logVerbosity":
                        logVerbosity = int.Parse(value);
                        break;
                    default:
                        // unknown key
                        Logging.warn("Unknown config parameter was specified '" + key + "'");
                        break;
                }
            }
        }

        public static void init(string[] args)
        {
            // first pass
            var cmd_parser = new FluentCommandLineParser();

            // help
            cmd_parser.SetupHelp("h", "help").Callback(text => outputHelp());

            // config file
            cmd_parser.Setup<string>("config").Callback(value => configFilename = value).Required();

            cmd_parser.Parse(args);

            readConfigFile(configFilename);

            processCliParmeters(args);
            Logging.verbosity = logVerbosity;
        }

        private static void processCliParmeters(string[] args)
        {
            // second pass
            var cmd_parser = new FluentCommandLineParser();

            string seedNode = "";

            // version
            cmd_parser.Setup<bool>('v', "version").Callback(text => outputVersion());

            cmd_parser.Setup<int>('p', "port").Callback(value => Config.serverPort = value).Required();

            cmd_parser.Setup<int>('a', "apiport").Callback(value => apiPort = value).Required();

            cmd_parser.Setup<string>('i', "ip").Callback(value => externalIp = value).Required();

            cmd_parser.Setup<string>('w', "wallet").Callback(value => walletFile = value).Required();

            cmd_parser.Setup<string>('n', "node").Callback(value => seedNode = value).Required();

            cmd_parser.Setup<int>("maxLogSize").Callback(value => maxLogSize = value).Required();

            cmd_parser.Setup<int>("maxLogCount").Callback(value => maxLogCount = value).Required();

            cmd_parser.Setup<int>("logVerbosity").Callback(value => logVerbosity = value).Required();

            cmd_parser.Setup<int>("maxOutgoingConnections").Callback(value => maxOutgoingConnections = value);

            cmd_parser.Parse(args);

            if (seedNode != "")
            {
                CoreNetworkUtils.seedNodes = new List<string[]>
                {
                    new string[2] {seedNode, null}
                };
            }
        }

        private static void outputVersion()
        {
            Console.WriteLine("version");
        }
    }
}
