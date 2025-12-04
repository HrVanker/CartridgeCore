using System;
using System.Threading;
using TTRPG.Client.Services; // Ensure this matches your Service namespace

namespace TTRPG.Client
{
    public static class Program
    {
        static void Main()
        {
            using (var game = new Game1())
                game.Run();
            Console.WriteLine("=== CLIENT STARTED (HEADLESS MODE) ===");

            // 1. Initialize the Network Service
            var clientService = new ClientNetworkService();

            // 2. Connect to Localhost
            // (Make sure Server is running first!)
            Console.WriteLine("Attempting to connect to 127.0.0.1:9050...");
            clientService.Connect("localhost", 9050);

            // 3. The "Game Loop" (Network Only)
            // We loop until the user presses a key, allowing time for packets to send/receive
            Console.WriteLine("Client running. Press ESC to quit.");

            bool isRunning = true;
            while (isRunning)
            {
                // This checks for incoming packets (vital!)
                clientService.Poll();

                // Prevent the CPU from running at 100% usage
                Thread.Sleep(15);

                // Check for exit
                if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape)
                {
                    isRunning = false;
                }
            }

            // 4. Cleanup
            clientService.Stop();
            Console.WriteLine("Client stopped.");
        }
    }
}