using Agents;
using Microsoft.KernelMemory;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Memory;

var OPENAI_APIKEY = Configuration.GetValue("OPENAI_APIKEY");
var OPENAI_CHAT_MODELID = Configuration.GetValue("OPENAI_CHAT_MODELID");
var OPENAI_EMBEDDING_MODELID = Configuration.GetValue("OPENAI_EMBEDDING_MODELID");
var AZURE_OPENAI_ENDPOINT = Configuration.GetValue("AZURE_OPENAI_ENDPOINT");
var AZURE_OPENAI_APIKEY = Configuration.GetValue("AZURE_OPENAI_APIKEY");
var AZURE_OPENAI_CHAT_MODELID = Configuration.GetValue("AZURE_OPENAI_CHAT_MODELID");
var AZURE_OPENAI_EMBEDDING_MODELID = Configuration.GetValue("AZURE_OPENAI_EMBEDDING_MODELID");

string memoryCollection = "daily-news-memory";
string[] newsSources =
{
    "http://rss.cnn.com/rss/cnn_topstories.rss",
    "https://feeds.bbci.co.uk/news/rss.xml",
    //"https://rss.nytimes.com/services/xml/rss/nyt/HomePage.xml"
};

var memory = new MemoryBuilder()
           .WithMemoryStore(new VolatileMemoryStore())
           .WithOpenAITextEmbeddingGeneration(OPENAI_EMBEDDING_MODELID, OPENAI_APIKEY)
           .Build();

Console.WriteLine("Fetching top news headlines...");

//foreach (var feedUrl in newsSources)
//{
//    var feed = await FeedReader.ReadAsync(feedUrl);
//    foreach (var item in feed.Items.Take(3))
//    {
//        var content = $"{item.Title}\n{item.Description}";
//        Console.WriteLine($"Saving to memory: {item.Title}");
//        await memory.SaveInformationAsync(memoryCollection, content, item.Link);
//    }
//}

await StoreMemoryAsync(memory);
await SearchMemoryAsync(memory, "How do I get started?");
await SearchMemoryAsync(memory, "Can I build a chat with SK?");

async Task SearchMemoryAsync(ISemanticTextMemory memory, string query)
{
    Console.WriteLine("\nQuery: " + query + "\n");

    var memoryResults = memory.SearchAsync(memoryCollection, query, limit: 2, minRelevanceScore: 0.5);

    int i = 0;
    await foreach (MemoryQueryResult memoryResult in memoryResults)
    {
        Console.WriteLine($"Result {++i}:");
        Console.WriteLine("  URL:     : " + memoryResult.Metadata.Id);
        Console.WriteLine("  Title    : " + memoryResult.Metadata.Description);
        Console.WriteLine("  Relevance: " + memoryResult.Relevance);
        Console.WriteLine();
    }

    Console.WriteLine("----------------------");
}

async Task StoreMemoryAsync(ISemanticTextMemory memory)
{
    Console.WriteLine("\nAdding some GitHub file URLs and their descriptions to the semantic memory.");
    var githubFiles = SampleData();
    var i = 0;
    foreach (var entry in githubFiles)
    {
        await memory.SaveReferenceAsync(
            collection: memoryCollection,
            externalSourceName: "GitHub",
            externalId: entry.Key,
            description: entry.Value,
            text: entry.Value);

        Console.Write($" #{++i} saved.");
    }

    Console.WriteLine("\n----------------------");
}

static Dictionary<string, string> SampleData()
{
    return new Dictionary<string, string>
    {
        ["https://github.com/microsoft/semantic-kernel/blob/main/README.md"]
            = "README: Installation, getting started, and how to contribute",
        ["https://github.com/microsoft/semantic-kernel/blob/main/dotnet/notebooks/02-running-prompts-from-file.ipynb"]
            = "Jupyter notebook describing how to pass prompts from a file to a semantic plugin or function",
        ["https://github.com/microsoft/semantic-kernel/blob/main/dotnet/notebooks/00-getting-started.ipynb"]
            = "Jupyter notebook describing how to get started with the Semantic Kernel",
        ["https://github.com/microsoft/semantic-kernel/tree/main/prompt_template_samples/ChatPlugin/ChatGPT"]
            = "Sample demonstrating how to create a chat plugin interfacing with ChatGPT",
        ["https://github.com/microsoft/semantic-kernel/blob/main/dotnet/src/Plugins/Plugins.Memory/VolatileMemoryStore.cs"]
            = "C# class that defines a volatile embedding store",
    };
}
