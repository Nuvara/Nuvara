using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace MinecraftServer
{
    class UpdatePacks
    {
        private static readonly string ModsFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mods");
        private static readonly string BackupFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mods_backup");
        private static readonly string UpdateUrlTemplate = "https://nuv.pack.bloxycola.online/{0}";

        public static async Task UpdateAllModsAsync()
        {
            if (!Directory.Exists(ModsFolderPath))
            {
                Console.WriteLine($"Mods folder '{ModsFolderPath}' not found.");
                return;
            }

            // Ensure backup folder exists
            Directory.CreateDirectory(BackupFolderPath);

            string[] modFiles = Directory.GetFiles(ModsFolderPath, "*.jar");
            if (modFiles.Length == 0)
            {
                Console.WriteLine("No .jar files found in mods folder to update.");
                return;
            }

            foreach (var modFile in modFiles)
            {
                string modName = Path.GetFileNameWithoutExtension(modFile);
                Console.WriteLine($"\nChecking for updates for mod: {modName}");

                bool updated = await UpdateModIfAvailableAsync(modFile, modName);
                if (updated)
                {
                    Console.WriteLine($"Mod '{modName}' was updated successfully.\n");
                }
                else
                {
                    Console.WriteLine($"No update needed for mod: {modName}\n");
                }
            }

            Console.WriteLine("Mod update process completed.");
        }

        private static async Task<bool> UpdateModIfAvailableAsync(string localModPath, string modName)
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

                    if (!onlineHash.Equals(localHash, StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"Update detected for {modName}. Creating a backup and updating...");

                        BackupModFile(localModPath);

                        // Replace the local mod with the updated one
                        await File.WriteAllBytesAsync(localModPath, onlineModData);
                        Console.WriteLine($"Mod '{modName}' updated to latest version.");

                        return true;
                    }
                    else
                    {
                        Console.WriteLine($"Mod '{modName}' is already up-to-date.");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error updating mod '{modName}': {ex.Message}");
                    return false;
                }
            }
        }

        private static void BackupModFile(string filePath)
        {
            try
            {
                string fileName = Path.GetFileName(filePath);
                string backupPath = Path.Combine(BackupFolderPath, fileName);

                File.Copy(filePath, backupPath, overwrite: true);
                Console.WriteLine($"Backup created for mod '{fileName}' at '{backupPath}'");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to create backup for '{filePath}': {ex.Message}");
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
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Starting mod update process...");
            await UpdatePacks.UpdateAllModsAsync();
            Console.WriteLine("Mod update process completed.");
        }
    }
}
