using TTRPG.Core.Engine;

namespace TTRPG.Core;

// This interface is the entry point for any Rules Cartridge.
// When the Server loads a DLL, it looks for a class that implements this.
public interface IRuleset
{
    string Name { get; }
    string Version { get; }

    // Called by the Server when the cartridge is mounted.
    // We will use this later to register Systems and Components.
    void Register(object world);

    ICombatResolver GetResolver();
    IUIProvider GetUI();
}