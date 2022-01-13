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

        public static double poolFee = 0.00;
        public static string poolFeeAddress = "";

        public static string poolUrl = "";

        public static bool noStart = false;

        public const int maxClientFailuresPerMinute = 60;

        public static int miningBlocksPoolSize = 100;

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
            Console.WriteLine("    --poolFee\t\t Specify the pool fee");
            Console.WriteLine("    --poolFeeAddress\t\t Specify the Ixian address where to collect fee");
            Console.WriteLine("    --poolUrl\t\t Specify the url that should be used to access the pool - used for display purpose only");
            Console.WriteLine("    --noStart\t\t Don't start API and sync processes at startup - can be started later from console interface");
            Console.WriteLine("    --blocksPoolSize\t\t Set pool size for blocks with lowest difficulty - mining block will be choose from these");
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

        private static bool readConfigFile(string filename)
        {
            if (!File.Exists(filename))
            {
                return true;
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
                        if (!int.TryParse(value, out apiPort))
                        {
                            Console.WriteLine("API port should be an integer positive number.");
                            return false;
                        }
                        if (apiPort < 0)
                        {
                            Console.WriteLine("API port should be an integer positive number.");
                            return false;
                        }
                        break;
                    case "pool":
                        if (!int.TryParse(value, out poolPort))
                        {
                            Console.WriteLine("Pool website port should be an integer positive number.");
                            return false;
                        }
                        if (poolPort < 0)
                        {
                            Console.WriteLine("Pool website port should be an integer positive number.");
                            return false;
                        }
                        break;
                    case "wallet":
                        walletFile = value;
                        break;
                    case "password":
                        walletPassword = value;
                        break;
                    case "difficulty":
                        if (!ulong.TryParse(value, out startingDifficulty))
                        {
                            Console.WriteLine("Difficulty should be an integer positive number.");
                            return false;
                        }
                        break;
                    case "sharesPerSec":
                        if (!int.TryParse(value, out targetSharesPerSecond))
                        {
                            Console.WriteLine("Target shares per second should be an integer positive number.");
                            return false;
                        }
                        if(targetSharesPerSecond < 0)
                        {
                            Console.WriteLine("Target shares per second should be an integer positive number.");
                            return false;
                        }
                        break;
                    case "poolFee":
                        if(!double.TryParse(value, out poolFee))
                        {
                            Console.WriteLine("Pool fee value should be a floating point number between 0 and 1.");
                            return false;
                        }
                        if(poolFee > 1 || poolFee < 0)
                        {
                            Console.WriteLine("Pool fee value should be a floating point number between 0 and 1.");
                            return false;
                        }

                        break;
                    case "poolFeeAddress":
                        poolFeeAddress = value;
                        break;

                    case "poolUrl":
                        poolUrl = value;
                        break;

                    case "noStart":
                        noStart = (value == "1") || (value.ToLower() == "t") || (value.ToLower() == "true");
                        break;

                    case "blocksPoolSize":
                        if (!int.TryParse(value, out miningBlocksPoolSize))
                        {
                            Console.WriteLine("Mining blocks pool size value should be an integer number between 1 and 200.");
                            return false;
                        }
                        if (miningBlocksPoolSize < 1 || miningBlocksPoolSize > 200)
                        {
                            Console.WriteLine("Mining blocks pool size value should be an integer number between 1 and 200.");
                            return false;
                        }
                        break;

                    default:
                        // unknown key
                        Console.WriteLine("Unknown config parameter was specified '" + key + "'");
                        return false;
                }
            }

            if(poolFee > 0 && String.IsNullOrEmpty(poolFeeAddress))
            {
                Console.WriteLine("Pool fee address must not be empty if pool fee is higher than 0.");
            }

            return true;
        }

        public static bool init(string[] args)
        {
            bool continueProcessing = true;

            if(!readConfigFile("ixian.cfg"))
            {
                return false;
            }

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

            cmd_parser.Setup<double>("poolFee").Callback(value => poolFee = value);
            cmd_parser.Setup<string>("poolFeeAddress").Callback(value => poolFeeAddress = value);

            cmd_parser.Setup<string>("poolUrl").Callback(value => poolUrl = value);

            cmd_parser.Setup<bool>("noStart").Callback(value => { noStart = true; });

            cmd_parser.Setup<int>("blocksPoolSize").Callback(value => miningBlocksPoolSize = value);

            cmd_parser.Parse(args);

            if(poolFee > 1 || poolFee < 0)
            {
                poolFee = 0.01;
            }

            if (miningBlocksPoolSize < 1 || miningBlocksPoolSize > 200)
            {
                miningBlocksPoolSize = 100;
            }

            return continueProcessing;
        }
    }
}
