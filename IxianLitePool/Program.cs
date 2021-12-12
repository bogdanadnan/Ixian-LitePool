using IXICore;
using IXICore.Meta;
using LP.Meta;
using System;
using System.Threading;
using LP.Network;
using LP.Pool;
using Microsoft.Owin.Hosting;

namespace IxianLitePool
{
    class Program
    {
        public static bool running = false;

        private static Node node = null;

        private static APIServer api = null;
        
        public static Commands commands = null;

        static void Main(string[] args)
        {
            // Clear the console first
            Console.Clear();

            Console.WriteLine("Ixian Lite Pool {0} ({1})", Config.version, CoreConfig.version);

            string domainAddress = "http://localhost/";

            using (WebApp.Start(url: domainAddress))
            {
                onStart(args);
                mainLoop();
                onStop();
            }
        }

        static void onStart(string[] args)
        {
            running = true;

            Config.init(args);
            
            commands = new Commands();

            // Initialize the node
            node = new Node();

            // Start the node
            node.start();

            api = new APIServer(node, Config.apiBinds, Config.apiUsers, Config.apiAllowedIps);
        }

        static void mainLoop()
        {
            Console.WriteLine("Type help to see a list of available commands.\n");
            while (running && !IxianHandler.forceShutdown)
            {
                Console.Write("IxianLitePool>");
                string line = Console.ReadLine();
                Console.WriteLine("");

                commands.handleCommand(line);
            }
        }
        
        static void onStop()
        {
            running = false;

            // Stop the DLT
            node.stop();

            // Stop logging
            Logging.flush();
            Logging.stop();
        }

        public static void stop()
        {
            running = false;
        }
    }
}
