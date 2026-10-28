using System;
using System.Threading;
using System.Threading.Tasks;
using ProjectName.Shared.Tv;

namespace ProjectName.Tv
{
    /// <summary>
    /// Google TV / Android TV Remote Control Console Application.
    /// Provides low-latency keyboard-driven remote control over local network for Android TV / Google TV.
    /// </summary>
    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            Console.Title = "[PROJECT_NAME] Google TV Remote Controller";
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("==================================================================");
            Console.WriteLine("  [PROJECT_NAME] - Google TV & Android TV Remote Control Console  ");
            Console.WriteLine("==================================================================");
            Console.ResetColor();
            Console.WriteLine();

            // Default target host (configured from detected local Xiaomi Mi TV or user input)
            string targetHost = "192.168.254.104";
            int targetPort = GoogleTvRemoteClient.DefaultControlPort; // 6467

            // Parse optional CLI arguments: --host <ip> --port <port>
            for (int i = 0; i < args.Length; i++)
            {
                if ((args[i] == "--host" || args[i] == "-h") && i + 1 < args.Length)
                {
                    targetHost = args[++i];
                }
                else if ((args[i] == "--port" || args[i] == "-p") && i + 1 < args.Length && int.TryParse(args[++i], out var p))
                {
                    targetPort = p;
                }
            }

            Console.WriteLine($"Default target TV: {targetHost}:{targetPort}");
            Console.Write("Press [Enter] to connect to default, or type a new TV IP address: ");
            string? customInput = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(customInput))
            {
                if (customInput.Contains(':'))
                {
                    var parts = customInput.Split(':');
                    targetHost = parts[0].Trim();
                    if (int.TryParse(parts[1].Trim(), out var p)) targetPort = p;
                }
                else
                {
                    targetHost = customInput.Trim();
                }
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n[TV-CONNECT] Connecting to Google TV at {targetHost}:{targetPort} over TLS...");
            Console.ResetColor();

            await using var controller = new TvRemoteController();
            controller.Disconnected += (s, reason) =>
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[TV-DISCONNECT] Connection closed by TV: {reason}");
                Console.ResetColor();
            };

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await controller.ConnectAsync(targetHost, targetPort, cts.Token);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[TV-CONNECTED] Successfully connected to TV at {targetHost}:{targetPort}!\n");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[TV-ERROR] Unable to connect to TV at {targetHost}:{targetPort}: {ex.Message}");
                Console.WriteLine("Note: Ensure Google TV / Android TV is powered on and connected to the same Wi-Fi subnet.");
                Console.ResetColor();
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
                return 1;
            }

            // Print Interactive Control Key Guide
            PrintKeyGuide();

            // Run interactive control loop
            bool running = true;
            while (running && controller.IsConnected)
            {
                var keyInfo = Console.ReadKey(intercept: true);

                try
                {
                    switch (keyInfo.Key)
                    {
                        case ConsoleKey.UpArrow:
                            Console.WriteLine(" > D-Pad UP");
                            await controller.DpadUpAsync();
                            break;

                        case ConsoleKey.DownArrow:
                            Console.WriteLine(" > D-Pad DOWN");
                            await controller.DpadDownAsync();
                            break;

                        case ConsoleKey.LeftArrow:
                            Console.WriteLine(" > D-Pad LEFT");
                            await controller.DpadLeftAsync();
                            break;

                        case ConsoleKey.RightArrow:
                            Console.WriteLine(" > D-Pad RIGHT");
                            await controller.DpadRightAsync();
                            break;

                        case ConsoleKey.Enter:
                            Console.WriteLine(" > SELECT / OK");
                            await controller.SelectAsync();
                            break;

                        case ConsoleKey.Escape:
                        case ConsoleKey.Backspace:
                            Console.WriteLine(" > BACK");
                            await controller.BackAsync();
                            break;

                        case ConsoleKey.H:
                            Console.WriteLine(" > HOME");
                            await controller.HomeAsync();
                            break;

                        case ConsoleKey.OemPlus:
                        case ConsoleKey.Add:
                            Console.WriteLine(" > VOLUME UP");
                            await controller.VolumeUpAsync();
                            break;

                        case ConsoleKey.OemMinus:
                        case ConsoleKey.Subtract:
                            Console.WriteLine(" > VOLUME DOWN");
                            await controller.VolumeDownAsync();
                            break;

                        case ConsoleKey.Spacebar:
                            Console.WriteLine(" > PLAY / PAUSE");
                            await controller.PlayPauseAsync();
                            break;

                        case ConsoleKey.Q:
                            Console.WriteLine(" > Disconnecting TV remote...");
                            running = false;
                            break;

                        case ConsoleKey.Help:
                        case ConsoleKey.F1:
                            PrintKeyGuide();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"[CMD-WARN] Command delivery error: {ex.Message}");
                    Console.ResetColor();
                }
            }

            Console.WriteLine("Exiting TV Remote controller. Goodbye.");
            return 0;
        }

        private static void PrintKeyGuide()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("+---------------------------------------------------------------+");
            Console.WriteLine("|                  INTERACTIVE REMOTE KEYBOARD GUIDE            |");
            Console.WriteLine("+---------------------------------------------------------------+");
            Console.WriteLine("|  [Up / Down / Left / Right] : D-Pad Directional Navigation    |");
            Console.WriteLine("|  [Enter]                    : Select / OK / Confirm           |");
            Console.WriteLine("|  [Escape / Backspace]       : Back                            |");
            Console.WriteLine("|  [H]                        : Home Screen                     |");
            Console.WriteLine("|  [+ / -]                    : Volume Up / Down                |");
            Console.WriteLine("|  [Spacebar]                 : Play / Pause Media              |");
            Console.WriteLine("|  [F1]                       : Re-display this guide           |");
            Console.WriteLine("|  [Q]                        : Disconnect and Quit             |");
            Console.WriteLine("+---------------------------------------------------------------+");
            Console.ResetColor();
            Console.WriteLine();
        }
    }
}
