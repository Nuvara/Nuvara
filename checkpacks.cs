using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using IKVM.Runtime;

namespace MinecraftServer
{
    class ModLoader
    {
        private static readonly string ModsFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mods");
        private static readonly string UpdateUrlTemplate = "https://nuv.pack.bloxycola.online/{0}";

        public static async Task LoadModsAsync()
        {
            if (!Directory.Exists(ModsFolderPath))
            {
                Console.WriteLine($"Mods folder '{ModsFolderPath}' not found.");
                return;
            }

            string[] modFiles = Directory.GetFiles(ModsFolderPath, "*.jar");
            if (modFiles.Length == 0)
            {
                Console.WriteLine("No .jar files found in mods folder.");
                return;
            }

            List<object> loadedMods = new List<object>();

            foreach (var modFile in modFiles)
            {
                string modName = Path.GetFileNameWithoutExtension(modFile);
                Console.WriteLine($"Checking for updates for mod: {modName}");
                
                // Check for updates
                if (await CheckForUpdateAsync(modFile, modName))
                {
                    Console.WriteLine($"Update found for {modName}, running updatepacks.cs...");
                    RunUpdateScript();
                }
                
                // Load mod
                try
                {
                    Console.WriteLine($"Loading mod: {modName}");
                    using (var fileStream = new FileStream(modFile, FileMode.Open))
                    {
                        var assembly = IKVM.Loader.LoadJar(fileStream);
                        loadedMods.Add(assembly);
                    }
                    Console.WriteLine($"Successfully loaded mod: {modName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load mod '{modName}': {ex.Message}");
                }
            }

            Console.WriteLine($"{loadedMods.Count} mods loaded successfully.");
        }

        private static async Task<bool> CheckForUpdateAsync(string localModPath, string modName)
        {
            string url = string.Format(UpdateUrlTemplate, modName);
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    byte[] onlineModData = await client.GetByteArrayAsync(url);
                    byte[] localModData = File.ReadAllBytes(localModPath);

                    string onlineHash = ComputeHash(onlineModData);
                    string localHash = ComputeHash(localModData);

                    return !onlineHash.Equals(localHash, StringComparison.OrdinalIgnoreCase);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error checking for update for {modName}: {ex.Message}");
                    return false;
                }
            }
        }

        private static string ComputeHash(byte[] data)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(data);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }

        private static void RunUpdateScript()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "run updatepacks.cs",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = Process.Start(startInfo))
            {
                using (StreamReader reader = process.StandardOutput)
                {
                    string result = reader.ReadToEnd();
                    Console.WriteLine(result);
                }
            }
        }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Starting mod loading process...");
            await ModLoader.LoadModsAsync();
            Console.WriteLine("Mod loading process completed.");
        }
    }
}
