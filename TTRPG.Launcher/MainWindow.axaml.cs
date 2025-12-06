using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using TTRPG.Shared.DTOs;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace TTRPG.Launcher
{
    public partial class MainWindow : Window
    {
        private List<CampaignManifest> _manifests = new List<CampaignManifest>();
        private CampaignManifest? _selectedManifest;

        public MainWindow()
        {
            InitializeComponent();
            LoadCartridges();
        }

        private void LoadCartridges()
        {
            // 1. Locate Data Directory (Relative to Launcher build)
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            // Walk up to Solution Root, then down to Server/Data
            // Note: In a real install, this would be a local "Games" folder
            string solutionDir = Path.GetFullPath(Path.Combine(baseDir, "../../../.."));
            string serverDataDir = Path.Combine(solutionDir, "TTRPG.Server", "Data");

            if (Directory.Exists(serverDataDir))
            {
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();

                string manifestPath = Path.Combine(serverDataDir, "manifest.yaml");
                if (File.Exists(manifestPath))
                {
                    try
                    {
                        var yaml = File.ReadAllText(manifestPath);
                        var manifest = deserializer.Deserialize<CampaignManifest>(yaml);
                        _manifests.Add(manifest);
                    }
                    catch { /* Log error */ }
                }
            }

            CartridgeList.ItemsSource = _manifests;
        }

        private void CartridgeList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (CartridgeList.SelectedItem is CampaignManifest manifest)
            {
                _selectedManifest = manifest;
                TitleText.Text = manifest.Name;
                AuthorText.Text = manifest.Author;

                string deps = "";
                foreach (var d in manifest.Dependencies) deps += $"{d.Key} (v{d.Value})\n";
                DepText.Text = string.IsNullOrEmpty(deps) ? "None" : deps;

                PlayButton.IsEnabled = true;
            }
        }

        private void PlayButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_selectedManifest == null) return;

            try
            {
                // 1. Resolve Executable Paths
                string solutionDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../.."));

                // Note: On Linux/Mac, these extensions won't be .exe
                string serverExe = Path.Combine(solutionDir, "TTRPG.Server", "bin", "Debug", "net8.0", "TTRPG.Server.exe");
                string clientExe = Path.Combine(solutionDir, "TTRPG.Client", "bin", "Debug", "net8.0", "TTRPG.Client.exe");

                // 2. Launch Server
                Process.Start(new ProcessStartInfo
                {
                    FileName = serverExe,
                    UseShellExecute = true, // Required to open a new terminal window
                    CreateNoWindow = false
                });

                // 3. Launch Client (Small delay to let server bind port)
                System.Threading.Thread.Sleep(1000);
                Process.Start(new ProcessStartInfo
                {
                    FileName = clientExe,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                // Avalonia doesn't have MessageBox built-in easily, print to console for now
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}