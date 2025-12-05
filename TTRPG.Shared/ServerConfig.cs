namespace TTRPG.Shared
{
    public class ServerConfig
    {
        // If True: Everyone hears everything (Discord style)
        // If False: You must be nearby/same zone (Proximity Chat)
        public bool IsCasualMode { get; set; } = true;
    }
}