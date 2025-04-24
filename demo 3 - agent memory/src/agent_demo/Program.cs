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

string feedbackCollection = "news_top_stories_feedback";
string newsCollection = "news_top_stories";

var memory = new MemoryBuilder()
           .WithMemoryStore(new VolatileMemoryStore())
           .WithOpenAITextEmbeddingGeneration(OPENAI_EMBEDDING_MODELID, OPENAI_APIKEY)
           .Build();

await StoreMemoryAsync(memory);
Console.WriteLine("How can I help you?");

while (true)
{
    Console.Write("> ");
    string query = Console.ReadLine() ?? "";

    if (query.StartsWith('_'))
    {
        Console.WriteLine("\nPlease share your feedback to optimize your news stories? (press Enter to skip)");
        var feedback = Console.ReadLine();

        if (!string.IsNullOrWhiteSpace(feedback))
        {
            var feedbackEntry = $"FEEDBACK on {DateTime.UtcNow:yyyy-MM-dd}: {feedback}";
            await memory.SaveInformationAsync(feedbackCollection, feedbackEntry, $"feedback-{Guid.NewGuid()}");
            query = "feedback";
        }
    }

    await SearchMemoryAsync(memory, query);
}

async Task SearchMemoryAsync(ISemanticTextMemory memory, string query)
{
    IAsyncEnumerable<MemoryQueryResult> memoryResults = null;
    IAsyncEnumerable<MemoryQueryResult> feedbackResults = null;

    if (!string.IsNullOrEmpty(query) && query == "feedback")
    {
        feedbackResults =
            memory.SearchAsync(
                feedbackCollection,
                "FEEDBACK",
                limit: 5);

        await PrintNewsAsync(feedbackResults!);
        return;
    }

    if (string.IsNullOrEmpty(query) || query == "list")
    {
        memoryResults =
            memory.SearchAsync(
                newsCollection,
                " ",
                limit: 20);

        await PrintNewsAsync(memoryResults!);
        return;
    }

    memoryResults =
        memory.SearchAsync(
            newsCollection,
            query,
            limit: 1,
            minRelevanceScore: 0.5);

    await PrintNewsAsync(memoryResults!);
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
                collection: newsCollection,
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

static async Task PrintNewsAsync(IAsyncEnumerable<MemoryQueryResult> memoryResults)
{
    await foreach (MemoryQueryResult memoryResult in memoryResults!.OrderByDescending(x => x.Relevance))
    {
        Console.WriteLine(memoryResult.Metadata.Text);
        Console.WriteLine(memoryResult.Metadata.Id);
        Console.WriteLine(memoryResult.Relevance);
        Console.WriteLine();
    }
}