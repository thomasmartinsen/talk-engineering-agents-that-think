using Agents;
using CodeHollow.FeedReader;
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

string memoryCollection = "collection";

var memory = new MemoryBuilder()
           .WithMemoryStore(new VolatileMemoryStore())
           .WithOpenAITextEmbeddingGeneration(OPENAI_EMBEDDING_MODELID, OPENAI_APIKEY)
           .Build();

await StoreMemoryAsync(memory);
Console.WriteLine("How can I help you?");
while (true)
{
    Console.Write("> ");
    var query = Console.ReadLine() ?? "Any news about Tesla?";
    await SearchMemoryAsync(memory, query);
}

async Task SearchMemoryAsync(ISemanticTextMemory memory, string query)
{
    var memoryResults =
        memory.SearchAsync(
            memoryCollection,
            query,
            limit: 1,
            minRelevanceScore: 0.5);

    await foreach (MemoryQueryResult memoryResult in memoryResults)
    {
        Console.WriteLine(memoryResult.Metadata.Text);
        Console.WriteLine(memoryResult.Metadata.Id);
        Console.WriteLine(memoryResult.Relevance);
        Console.WriteLine();
    }
}

async Task StoreMemoryAsync(ISemanticTextMemory memory)
{
    Console.WriteLine("\nAdding lastest top stories from major news sources to the semantic memory.");
    var entries = await GetTopStoriesAsync();

    var i = 0;
    foreach (var entry in entries)
    {
        foreach (var item in entry.Value)
        {
            await memory.SaveInformationAsync(
                collection: memoryCollection,
                id: item.Id,
                text: item.Title);
        }

        Console.WriteLine($" #{++i} saved.");
    }
}

static async Task<Dictionary<string, IEnumerable<FeedItem>>> GetTopStoriesAsync()
{
    var newsSources = new Dictionary<string, string>
    {
        ["CNN Top Stories"] = "http://rss.cnn.com/rss/cnn_topstories.rss",
        ["BBC Top Stories"] = "https://feeds.bbci.co.uk/news/rss.xml",
        ["NY Times Top Stories"] = "https://rss.nytimes.com/services/xml/rss/nyt/HomePage.xml",
    };

    var result = new Dictionary<string, IEnumerable<FeedItem>>();

    foreach (var feedUrl in newsSources)
    {
        var feed = await FeedReader.ReadAsync(feedUrl.Value);
        var content = feed.Items.Take(5);
        result.Add(feedUrl.Key, content);
    }

    return result;
}
