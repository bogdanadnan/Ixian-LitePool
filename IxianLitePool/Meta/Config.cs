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
        public static readonly string version = "xlp-0.0.1";

        public static string walletFile = "ixian.wal";
        public static string walletPassword = "";

        public static int poolPort = 8080;
        public static int apiPort = 8081;

        public static ulong startingDifficulty = 10000;
        public static int targetSharesPerSecond = 10;

        private Config()
        {

        }

        private static string outputHelp()
        {
            Console.WriteLine("Starts a new instance of Ixian Lite Pool");
            Console.WriteLine("");
            Console.WriteLine(
                " IxianLitePool.exe [--api 8081] [--pool 8080] [--wallet ixian.wal] [--password password]");
            Console.WriteLine("");
            Console.WriteLine("    -h\t\t\t Displays this help");
            Console.WriteLine("    -v\t\t\t Displays version");
            Console.WriteLine("    --api\t\t API port to listen on - used by mining software");
            Console.WriteLine("    --pool\t\t HTTP port to listen on - this will server pool website files");
            Console.WriteLine("    --wallet\t\t Specify location of the ixian.wal file");
            Console.WriteLine("    --password\t\t Specify the password for the wallet (be careful with this)");
            Console.WriteLine("    --difficulty\t\t Specify the starting difficulty for the pool");
            Console.WriteLine("    --sharesPerSec\t\t Specify the target shares per second accepted by the pool");
            Console.WriteLine("----------- Config File Options -----------");
            Console.WriteLine(" Config file options should use parameterName = parameterValue syntax.");
            Console.WriteLine(" Config file options are stored in ixan.cfg file.");
            Console.WriteLine(" Each option should be specified in its own line. Example:");
            Console.WriteLine("    api = 8081");
            Console.WriteLine("    pool = 8080");
            Console.WriteLine("");
            Console.WriteLine(" Same options are available for config file as for command line arguments");

            return "";
        }

        private static void outputVersion()
        {
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
                    case "api":
                        apiPort = int.Parse(value);
                        break;
                    case "pool":
                        poolPort = int.Parse(value);
                        break;
                    case "wallet":
                        walletFile = value;
                        break;
                    case "password":
                        walletPassword = value;
                        break;
                    case "difficulty":
                        startingDifficulty = ulong.Parse(value);
                        break;
                    case "sharesPerSec":
                        targetSharesPerSecond = int.Parse(value);
                        break;
                    default:
                        // unknown key
                        Logging.warn("Unknown config parameter was specified '" + key + "'");
                        break;
                }
            }
        }

        public static bool init(string[] args)
        {
            bool continueProcessing = true;

            readConfigFile("ixian.cfg");

            var cmd_parser = new FluentCommandLineParser();

            // help
            cmd_parser.SetupHelp("h", "help").Callback(text => { outputHelp(); continueProcessing = false; });

            // version
            cmd_parser.Setup<bool>('v', "version").Callback(text => { outputVersion(); continueProcessing = false; });

            // api port
            cmd_parser.Setup<int>("api").Callback(value => apiPort = value);

            // pool port
            cmd_parser.Setup<int>("pool").Callback(value => poolPort = value);

            // wallet
            cmd_parser.Setup<string>("wallet").Callback(value => walletFile = value);

            // password
            cmd_parser.Setup<string>("password").Callback(value => walletPassword = value);

            // starting difficulty
            cmd_parser.Setup<long>("difficulty").Callback(value => startingDifficulty = (ulong)value);

            // target shares per second
            cmd_parser.Setup<int>("sharesPerSec").Callback(value => targetSharesPerSecond = value);

            cmd_parser.Parse(args);

            return continueProcessing;
        }
    }
}
