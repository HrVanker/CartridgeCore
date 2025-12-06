using System;
using System.IO;
using System.Collections.Generic;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema; // Note: We will use a lightweight logic if this specific namespace is missing or use JSchema

namespace TTRPG.CLI
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== TTRPG Cartridge Validator ===");

            if (args.Length == 0)
            {
                Console.WriteLine("Usage: TTRPG.CLI [path_to_yaml_file]");
                return;
            }

            string targetFile = args[0];
            if (!File.Exists(targetFile))
            {
                Console.Error.WriteLine($"[Error] File not found: {targetFile}");
                Environment.Exit(1);
            }

            // 1. Determine Type
            string fileName = Path.GetFileName(targetFile).ToLower();
            if (fileName.Contains("manifest"))
            {
                ValidateManifest(targetFile);
            }
            else
            {
                Console.WriteLine("[Info] Only 'manifest.yaml' validation is currently supported.");
            }
        }

        static void ValidateManifest(string filePath)
        {
            try
            {
                Console.WriteLine($"Validating Manifest: {filePath}...");

                // 1. Read YAML
                var yamlContent = File.ReadAllText(filePath);
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();

                // Convert YAML -> Dynamic Object -> JSON String
                var yamlObject = deserializer.Deserialize<object>(yamlContent);
                var jsonString = JsonConvert.SerializeObject(yamlObject);

                // 2. Load Schema
                string schemaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Schemas", "ManifestSchema.json");
                if (!File.Exists(schemaPath))
                {
                    Console.Error.WriteLine("[Error] Schema definition not found.");
                    return;
                }

                // 3. Validate (Using Newtonsoft JToken + JSchema logic)
                // Note: Full 'JSchema' is a paid library in newer versions, 
                // so we will write a lightweight manual check or use the basic validation available.
                // For this implementation, we will perform structural checking manually to keep it free/MIT.

                JObject json = JObject.Parse(jsonString);
                bool isValid = true;

                // CHECK: Required Fields
                if (json["name"] == null || string.IsNullOrWhiteSpace(json["name"].ToString()))
                {
                    Console.Error.WriteLine("  [Fail] Missing required property: 'name'");
                    isValid = false;
                }
                if (json["version"] == null)
                {
                    Console.Error.WriteLine("  [Fail] Missing required property: 'version'");
                    isValid = false;
                }
                if (json["author"] == null)
                {
                    Console.Error.WriteLine("  [Fail] Missing required property: 'author'");
                    isValid = false;
                }

                // CHECK: Version Format
                /*
                if (json["version"] != null && !System.Text.RegularExpressions.Regex.IsMatch(json["version"].ToString(), @"^\d+\.\d+\.\d+$"))
                {
                     Console.Error.WriteLine("  [Fail] Version must be in format X.Y.Z (e.g. 1.0.0)");
                     isValid = false;
                }
                */

                if (isValid)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("  [Success] Manifest is valid.");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("  [Fail] Validation failed.");
                    Console.ResetColor();
                    Environment.Exit(1); // Return error code for CI/CD
                }

            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Error] Parsing failed: {ex.Message}");
                Environment.Exit(1);
            }
        }
    }
}