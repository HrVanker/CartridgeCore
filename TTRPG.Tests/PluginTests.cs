using Xunit;
using TTRPG.Server.Services;
using TTRPG.Core;
using System.IO;

namespace TTRPG.Tests;

public class PluginTests
{
    [Fact]
    public void PluginLoader_ShouldLoadSampleRules_WhenDllExists()
    {
        // Arrange
        // We know the build system copies "TTRPG.Rules.Sample.dll" to our output folder
        string dllName = "TTRPG.Rules.Sample.dll";
        string fullPath = Path.GetFullPath(dllName);
        var loader = new PluginLoader();

        // Act
        IRuleset loadedRules = loader.LoadRuleset(fullPath);

        // Assert
        Assert.NotNull(loadedRules);
        Assert.Equal("Dungeons of Unit Testing", loadedRules.Name);
        Assert.Equal("1.0.0", loadedRules.Version);
    }
}