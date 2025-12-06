using Xunit;
using TTRPG.Server.Services;
using TTRPG.Core;
using System.IO;

namespace TTRPG.Tests;

public class PluginTests
{
    [Fact]
    public void PluginLoader_ShouldLoadPathfinderRules_WhenDllExists()
    {
        // Arrange
        // We renamed the project, so the build output is now this:
        string dllName = "TTRPG.Rules.Pathfinder.dll";
        string fullPath = Path.GetFullPath(dllName);
        var loader = new PluginLoader();

        // Act
        IRuleset loadedRules = loader.LoadRuleset(fullPath);

        // Assert
        Assert.NotNull(loadedRules);
        Assert.Equal("Pathfinder Core Rules", loadedRules.Name);

        // Verify the resolver hook works
        Assert.NotNull(loadedRules.GetResolver());
    }
}