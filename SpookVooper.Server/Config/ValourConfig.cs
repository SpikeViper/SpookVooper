namespace SpookVooper.Server.Config;

public class ValourConfig
{
    public static ValourConfig Instance { get; set; }
    
    public string BotEmail { get; set; }
    public string BotPassword { get; set; }

    public ValourConfig()
    {
        Instance = this;
    }
}