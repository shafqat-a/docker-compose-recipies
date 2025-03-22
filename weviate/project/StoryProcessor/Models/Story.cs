using System.Collections.Generic;

namespace StoryProcessor.Models
{
    public class Story
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string Summary { get; set; }
        public List<string> Tags { get; set; }
        public bool IsStory { get; set; }

        public Story()
        {
            Tags = new List<string>();
        }
    }
} 