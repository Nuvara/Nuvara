using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace MinecraftServer
{
    public class Censor
    {
        private static readonly string CensorWordsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "censorwords.yml");
        private HashSet<string> censoredWords;

        public Censor()
        {
            LoadCensoredWords();
        }

        private void LoadCensoredWords()
        {
            if (!File.Exists(CensorWordsFilePath))
            {
                Console.WriteLine("Censor words file not found. Please ensure 'censorwords.yml' is in the server directory.");
                censoredWords = new HashSet<string>();
                return;
            }

            try
            {
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();

                using (var reader = new StreamReader(CensorWordsFilePath))
                {
                    var yamlContent = deserializer.Deserialize<Dictionary<string, List<string>>>(reader);
                    if (yamlContent != null && yamlContent.ContainsKey("censored_words"))
                    {
                        censoredWords = new HashSet<string>(yamlContent["censored_words"], StringComparer.OrdinalIgnoreCase);
                        Console.WriteLine($"Loaded {censoredWords.Count} censored words.");
                    }
                    else
                    {
                        Console.WriteLine("No censored words found in the configuration file.");
                        censoredWords = new HashSet<string>();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading censor words: {ex.Message}");
                censoredWords = new HashSet<string>();
            }
        }

        public string CheckMessage(string message, string username)
        {
            var words = message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var containsCensoredWords = words.Any(word => censoredWords.Contains(word, StringComparer.OrdinalIgnoreCase));

            if (containsCensoredWords)
            {
                Console.WriteLine($"Message from {username} contains censored words and has been removed.");
                return $"Your message was removed because it contains a prohibited word.";
            }

            return message;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Censor censor = new Censor();

            // Simulate chat input
            Console.WriteLine("Enter a chat message (or 'exit' to quit):");
            while (true)
            {
                Console.Write("> ");
                string input = Console.ReadLine();
                if (input.ToLower() == "exit") break;

                string username = "User123"; // Example username
                string result = censor.CheckMessage(input, username);

                if (result == input)
                {
                    Console.WriteLine($"[Chat] {username}: {result}");
                }
                else
                {
                    Console.WriteLine(result);
                }
            }
        }
    }
}
