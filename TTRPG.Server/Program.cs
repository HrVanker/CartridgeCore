using System;
using System.Threading;
using TTRPG.Server.Services;

namespace TTRPG.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== SERVER STARTED ===");

            var serverService = new ServerNetworkService();

            // CRITICAL STEP: Actually start listening!
            serverService.Start(9050);

            Console.WriteLine("Press ESC to stop...");

            bool isRunning = true;
            while (isRunning)
            {
                // CRITICAL STEP: You must Poll events or the server won't "read" the network card
                serverService.Poll();

                Thread.Sleep(15); // Prevent 100% CPU usage

                if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape)
                {
                    isRunning = false;
                }
            }

            serverService.Stop();
        }
    }
}