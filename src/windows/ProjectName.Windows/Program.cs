using System;
using System.Threading;
using System.Threading.Tasks;
using ProjectName.Shared.Discovery;
using ProjectName.Shared.Protocol;
using ProjectName.Tv;
using ProjectName.Windows.Endpoint;

namespace ProjectName.Windows
{
    /// <summary>
    /// [PROJECT_NAME] Windows Desktop Application Entry Point.
    /// Manages the desktop screen capture endpoint, secure LAN pairing, remote input receiver,
    /// mDNS discovery beacon, and integrated Google TV remote navigation.
    /// </summary>
    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            Console.Title = "[PROJECT_NAME] Windows Desktop Remote Endpoint & Controller";
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("========================================================================");
            Console.WriteLine("    [PROJECT_NAME] - Windows Desktop Endpoint & Remote System (v1.0)    ");
            Console.WriteLine("========================================================================");
            Console.ResetColor();
            Console.WriteLine();

            // 1. Initialize Windows Desktop Endpoint (Capture, Input Injector, TLS Server)
            await using var endpoint = new WindowsDesktopEndpoint();

            endpoint.EmergencyDisconnectTriggered += (s, reason) =>
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[EMERGENCY-STOP] Active session terminated: {reason}");
                Console.WriteLine("[EMERGENCY-STOP] All held keys and mouse buttons were reset to neutral.");
                Console.ResetColor();
            };

            Console.WriteLine("[INIT] Starting Windows Desktop Endpoint...");
            try
            {
                await endpoint.StartAsync();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[FATAL] Failed to start desktop endpoint: {ex.Message}");
                Console.ResetColor();
                return 1;
            }

            int listenPort = endpoint.ControlServer.Port;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[ONLINE] Desktop endpoint active on TLS Port: {listenPort}");
            Console.ResetColor();

            // 2. Start mDNS Discovery Beacon for LAN peers (phones, TVs)
            var beacon = new MdnsDiscoveryBeacon(endpoint.SessionCoordinator.LocalDevicePayload);

            // Wire automatic pairing challenge display
            endpoint.SessionCoordinator.PairingChallengePresented += (s, e) =>
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine($"\n[PAIRING-CHALLENGE] Device '{e.DeviceId}' is requesting connection!");
                Console.WriteLine($"[PAIRING-CHALLENGE] Confirm PIN on your phone: >>> {e.PinCode} <<< (Expires in {e.ExpiresInSeconds}s)");
                Console.ResetColor();
            };

            try
            {
                await beacon.StartAsync();
                Console.WriteLine("[mDNS] Local LAN discovery beacon broadcasting '_projectname._tcp'.");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[mDNS-WARN] Multicast beacon warning: {ex.Message} (Direct IP connect still supported).");
                Console.ResetColor();
            }

            Console.WriteLine();
            PrintStatus(endpoint, listenPort);
            PrintMenu();

            // 3. User Interaction Loop
            bool running = true;
            ConsoleCancelEventHandler cancelHandler = (s, e) =>
            {
                e.Cancel = true;
                running = false;
            };
            Console.CancelKeyPress += cancelHandler;

            while (running)
            {
                if (!Console.KeyAvailable)
                {
                    await Task.Delay(100);
                    continue;
                }

                var key = Console.ReadKey(intercept: true).Key;
                switch (key)
                {
                    case ConsoleKey.P:
                        // Generate a pairing PIN for incoming Android phone controller
                        var pin = PairingSession.GenerateNumericPin();
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.WriteLine($"\n[PAIRING-PIN] Generated temporary PIN: >>> {pin} <<< (Valid for 60 seconds)");
                        Console.WriteLine("Enter this PIN on your Android phone to authorize remote viewing and control.");
                        Console.ResetColor();
                        break;

                    case ConsoleKey.T:
                        // Launch integrated Google TV remote control session
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine("\n[TV-LAUNCH] Launching Google TV Remote Control Console...");
                        Console.ResetColor();
                        try
                        {
                            await ProjectName.Tv.Program.Main(Array.Empty<string>());
                        }
                        catch (Exception tvEx)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"[TV-ERROR] TV Remote error: {tvEx.Message}");
                            Console.ResetColor();
                        }
                        Console.WriteLine();
                        PrintMenu();
                        break;

                    case ConsoleKey.E:
                    case ConsoleKey.Escape:
                        // Immediate Emergency Disconnect & Key Release
                        await endpoint.EmergencyDisconnectAsync("User initiated emergency stop via keyboard.");
                        break;

                    case ConsoleKey.S:
                    case ConsoleKey.F1:
                        PrintStatus(endpoint, listenPort);
                        PrintMenu();
                        break;

                    case ConsoleKey.Q:
                        Console.WriteLine("\n[SHUTDOWN] Stopping Windows Desktop Endpoint...");
                        running = false;
                        break;
                }
            }

            // 4. Cleanup
            Console.CancelKeyPress -= cancelHandler;
            await beacon.DisposeAsync();
            await endpoint.StopAsync();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("[EXIT] Windows Desktop Endpoint stopped cleanly. Goodbye.");
            Console.ResetColor();
            return 0;
        }

        private static void PrintStatus(WindowsDesktopEndpoint endpoint, int port)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("+----------------------------------------------------------------------+");
            Console.WriteLine("|                       DESKTOP ENDPOINT STATUS                        |");
            Console.WriteLine("+----------------------------------------------------------------------+");
            Console.WriteLine($"|  Computer Name        : {Environment.MachineName,-44} |");
            Console.WriteLine($"|  Local IP / Port      : 0.0.0.0:{port,-36} |");
            Console.WriteLine($"|  Screen Capture       : 1920x1080 (Bounded Buffer, Zero Disk Storage)  |");
            Console.WriteLine($"|  Input Injection      : Windows SendInput (Active & Safeguarded)     |");
            Console.WriteLine("+----------------------------------------------------------------------+");
            Console.ResetColor();
            Console.WriteLine();
        }

        private static void PrintMenu()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Available Desktop Actions:");
            Console.WriteLine("  [P] Generate New 6-Digit Pairing PIN for Phone Controller");
            Console.WriteLine("  [T] Launch Google TV Remote Controller (Control Mi TV / Android TV)");
            Console.WriteLine("  [E] Emergency Disconnect (Abort session & release all keys)");
            Console.WriteLine("  [S] Refresh Endpoint Status");
            Console.WriteLine("  [Q] Shutdown and Exit");
            Console.ResetColor();
            Console.WriteLine();
        }
    }
}
