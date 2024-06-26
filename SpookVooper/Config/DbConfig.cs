namespace SpookVooper.Config;

public class DbConfig
{
    public static DbConfig Instance;

    public string Host { get; set; }

    public string Password { get; set; }

    public string Username { get; set; }

    public string Database { get; set; }

    public DbConfig()
    {
        // Set main instance to the most recently created config
        Instance = this;
    }
}