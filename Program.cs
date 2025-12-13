using WeatherBot;

public class Program
{
    public static async Task Main(string[] args)
    {
        var tokenPath = "token.txt";
        string botToken;

        try
        {
            if (!File.Exists(tokenPath))
            {
                if (File.Exists("../token.txt")) tokenPath = "../token.txt";
                else if (File.Exists("../../token.txt")) tokenPath = "../../token.txt";
                else if (File.Exists("../../../token.txt")) tokenPath = "../../../token.txt";
            }
            
            if (!File.Exists(tokenPath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Could not find {tokenPath}");
                Console.ResetColor();
                return;
            }

            botToken = await File.ReadAllTextAsync(tokenPath);
            botToken = botToken.Trim();
        }
        catch
        {
            Console.WriteLine($"❌ Error reading bot token from {tokenPath}!");
            return;
        }

        if (string.IsNullOrWhiteSpace(botToken))
        {
            Console.WriteLine("❌ Bot token is missing or empty.");
            return;
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("🤖 Starting Weather Bot...");
        Console.ResetColor();
        
        var app = new WeatherBotApplication(botToken);
        await app.RunAsync();
    }
}