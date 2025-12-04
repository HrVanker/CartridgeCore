using System.Reflection;
using TTRPG.Core;

namespace TTRPG.Server.Services;

public class PluginLoader
{
    public IRuleset LoadRuleset(string dllPath)
    {
        if (!File.Exists(dllPath))
            throw new FileNotFoundException($"Rules cartridge not found at: {dllPath}");

        // 1. Load the assembly (DLL) into memory
        Assembly assembly = Assembly.LoadFrom(dllPath);

        // 2. Scan the assembly for any class that implements IRuleset
        var rulesetType = assembly.GetTypes()
            .FirstOrDefault(t => typeof(IRuleset).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        if (rulesetType == null)
            throw new InvalidOperationException($"No IRuleset found in {dllPath}. Is this a valid Cartridge?");

        // 3. Create an instance of that class
        var ruleset = (IRuleset)Activator.CreateInstance(rulesetType)!;

        return ruleset;
    }
}