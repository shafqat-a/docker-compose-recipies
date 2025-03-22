using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
using StoryProcessor.Models;
using StoryProcessor.Services;

namespace StoryProcessor
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Please provide the path to the RAR file as an argument.");
                return;
            }

            var rarFilePath = args[0];
            if (!File.Exists(rarFilePath))
            {
                Console.WriteLine($"File not found: {rarFilePath}");
                return;
            }

            try
            {
                Console.WriteLine($"Processing RAR file: {rarFilePath}");
                var ollamaService = new OllamaService();
                var weaviateService = new WeaviateService();

                using (var archive = RarArchive.Open(rarFilePath))
                {
                    foreach (var entry in archive.Entries)
                    {
                        if (!entry.IsDirectory)
                        {
                            Console.WriteLine("\n========================================");
                            Console.WriteLine($"Processing file: {entry.Key}");
                            Console.WriteLine($"Full path: {Path.GetFullPath(entry.Key)}");
                            Console.WriteLine($"Size: {entry.Size:N0} bytes");
                            Console.WriteLine("========================================\n");
                            
                            // Read the file content
                            using var reader = new StreamReader(entry.OpenEntryStream(), Encoding.UTF8);
                            var content = await reader.ReadToEndAsync();

                            // Create a story object
                            var story = new Story
                            {
                                Title = Path.GetFileNameWithoutExtension(entry.Key),
                                Content = content
                            };

                            // Check if it's a story
                            Console.WriteLine("Calling Ollama to analyze if content is a story...");
                            //story.IsStory = await ollamaService.IsStoryContent(content);
                            
                            if (story.IsStory)
                            {
                                // Generate summary and tags
                                Console.WriteLine("Calling Ollama to generate summary...");
                                story.Summary = await ollamaService.GenerateSummary(content);
                                

                                Console.WriteLine("Calling Ollama to generate tags...");
                                story.Tags.AddRange(await ollamaService.GenerateTags(content));

                                // Save to Weaviate
                                Console.WriteLine("Saving to Weaviate...");
                                await weaviateService.SaveStory(story);
                                
                                Console.WriteLine(story.Summary);
                                Console.WriteLine(story.Tags);

                                Console.WriteLine($"Successfully processed and saved story: {story.Title}");
                                Console.WriteLine($"Generated tags: {string.Join(", ", story.Tags)}");
                            }
                            else
                            {
                                Console.WriteLine($"Skipping {entry.Key} as it's not identified as a story");
                            }

                            Console.WriteLine("-------------------");
                        }
                    }
                }

                Console.WriteLine("\nProcessing completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}
