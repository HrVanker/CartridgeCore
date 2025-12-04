using System;
using System.Collections.Generic;
using System.IO;
using TTRPG.Shared.DTOs;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace TTRPG.Server.Services
{
    public class BlueprintLoader
    {
        private readonly IDeserializer _deserializer;

        public BlueprintLoader()
        {
            // Configure YamlDotNet to ignore case (e.g., "Components" matches "components")
            _deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
        }

        public List<EntityBlueprint> LoadBlueprints(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"[Error] Blueprint file not found: {filePath}");
                return new List<EntityBlueprint>();
            }

            try
            {
                var yamlText = File.ReadAllText(filePath);

                // The YAML file is a list of blueprints (starts with "- id: ...")
                var blueprints = _deserializer.Deserialize<List<EntityBlueprint>>(yamlText);

                Console.WriteLine($"[Loader] Successfully loaded {blueprints.Count} blueprints from {Path.GetFileName(filePath)}.");
                return blueprints;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] Failed to parse blueprints: {ex.Message}");
                // In a real app, we might throw this up the stack, but for now we log it.
                return new List<EntityBlueprint>();
            }
        }

        public CampaignManifest LoadManifest(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"[Loader] Warning: Manifest not found at {filePath}");
                return new CampaignManifest { Name = "Unknown Campaign" };
            }

            try
            {
                var yamlText = File.ReadAllText(filePath);
                return _deserializer.Deserialize<CampaignManifest>(yamlText);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Loader] Error parsing manifest: {ex.Message}");
                return new CampaignManifest();
            }
        }
    }
}