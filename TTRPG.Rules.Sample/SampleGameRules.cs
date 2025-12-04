using TTRPG.Core;

namespace TTRPG.Rules.Sample;

public class SampleGameRules : IRuleset
{
    public string Name => "Dungeons of Unit Testing";
    public string Version => "1.0.0";

    public void Register(object world)
    {
        Console.WriteLine($"[Rules] {Name} v{Version} initialized!");
    }
}