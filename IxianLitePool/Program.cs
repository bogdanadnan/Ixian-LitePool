using IXICore;
using IXICore.Meta;
using LP.Meta;
using System;
using System.Threading;
using LP.Network;
using LP.Pool;
using Microsoft.Owin.Hosting;
using System.Collections.Generic;
using LP.Helpers;

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
            Console.WriteLine("Ixian Lite Pool {0} ({1})", Config.version, CoreConfig.version);

            if (!Config.init(args))
            {
                return;
            }

            string domainAddress = String.Format("http://*:{0}/", Config.poolPort);

            using (WebApp.Start(url: domainAddress))
            {
                if(!onStart(args))
                {
                    return;
                }

                mainLoop();
                onStop();
            }
        }

        static bool onStart(string[] args)
        {
            running = true;

            // Initialize the node
            node = new Node();

            commands = new Commands(node);

            // Start the node
            node.start();

            Payment.Instance.start(node);

            api = new APIServer(node, new List<String>() { String.Format("http://*:{0}/", Config.apiPort) });

            return true;
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

            api.stop();

            Payment.Instance.stop();
            IxiPrice.Instance.stop();

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
