using Watchmen.Infraestructure;

namespace Watchmen;

public class Program
{
    public static async Task Main(string[] args)
    {
        string? command = args.FirstOrDefault();

        switch (command)
        {
            case "migrate":
                await Migration.RunMigrationsAsync(args);
                break;
            case "serve":
                {
                    await using var server = new Server(args);
                    await server.RunAsync();
                }
                break;
            case "help":
            case "--help":
            case "-h":
                ShowHelp();
                break;

            default:
                Console.WriteLine("Invalid Option.");
                break;
        }
    }

    private static void ShowHelp()
    {
        Console.WriteLine(@"
        Usage:
            migrate             Run database migrations.
            serve               Start the server.
            help -h --help      Show this help message.
        ");
    }
}