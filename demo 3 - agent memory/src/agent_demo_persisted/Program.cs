using Agents;
using Agents.Models;
using CodeHollow.FeedReader;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.KernelMemory;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings;

var OPENAI_APIKEY = Configuration.GetValue("OPENAI_APIKEY");
var OPENAI_CHAT_MODELID = Configuration.GetValue("OPENAI_CHAT_MODELID");
var OPENAI_EMBEDDING_MODELID = Configuration.GetValue("OPENAI_EMBEDDING_MODELID");
var AZURE_OPENAI_ENDPOINT = Configuration.GetValue("AZURE_OPENAI_ENDPOINT");
var AZURE_OPENAI_APIKEY = Configuration.GetValue("AZURE_OPENAI_APIKEY");
var AZURE_OPENAI_CHAT_MODELID = Configuration.GetValue("AZURE_OPENAI_CHAT_MODELID");
var AZURE_OPENAI_EMBEDDING_MODELID = Configuration.GetValue("AZURE_OPENAI_EMBEDDING_MODELID");

string VECTOR_STORE_HOST = "localhost";
string USER_COLLECTION = "user_data";
string NEWS_COLLECTION = "news_articles";

var builder = Kernel.CreateBuilder();
builder.Services.AddAzureOpenAIChatCompletion(AZURE_OPENAI_CHAT_MODELID, AZURE_OPENAI_ENDPOINT, AZURE_OPENAI_APIKEY);
builder.Services.AddOpenAITextEmbeddingGeneration(OPENAI_EMBEDDING_MODELID, OPENAI_APIKEY);
builder.Services.AddQdrantVectorStore(VECTOR_STORE_HOST);
var kernel = builder.Build();

var vectorStore = kernel.GetRequiredService<IVectorStore>();
_ = vectorStore ?? throw new ArgumentNullException(nameof(vectorStore));

var embeddingService = kernel.GetRequiredService<ITextEmbeddingGenerationService>();
_ = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));

var newsCollection = vectorStore.GetCollection<Guid, NewsArticle>(NEWS_COLLECTION);
await newsCollection.CreateCollectionIfNotExistsAsync();

var userCollection = vectorStore.GetCollection<Guid, UserData>(USER_COLLECTION);
await userCollection.CreateCollectionIfNotExistsAsync();

var newsArticles = await FetchNewsArticlesAsync(true);
await PersistUserData(UserDataType.Interaction, "Fetched news");

var chatService = kernel.GetRequiredService<IChatCompletionService>();

Console.WriteLine("Ask about today’s news or in my memory: ");
while (true)
{
    Console.Write("> ");
    string query = Console.ReadLine() ?? "";
    Console.WriteLine();

    if (query.StartsWith("fact: "))
    {
        await PersistUserData(UserDataType.Fact, query[5..]);
    }

    if (query.StartsWith("exclude: "))
    {
        await PersistUserData(UserDataType.Exclude, query[8..]);
    }

    if (query.StartsWith("include: "))
    {
        await PersistUserData(UserDataType.Include, query[8..]);
    }

    var queryEmbeddings = await embeddingService!.GenerateEmbeddingAsync(query);

    var newsOptions = new VectorSearchOptions<NewsArticle> { Top = 3 };
    var newsItems = await newsCollection.VectorizedSearchAsync(queryEmbeddings, newsOptions);
    await newsItems.Results.ForEachAsync(x => PrintNewsArticle(x));

    var userOptions = new VectorSearchOptions<UserData>
    {
        Top = 3,
        Filter = x =>
            x.Kind == "Fact"
    };
    var userItems = await userCollection.VectorizedSearchAsync(queryEmbeddings, userOptions);
    await userItems.Results.ForEachAsync(x => PrintUserData(x));
}

async Task PersistUserData(UserDataType type, string text)
{
    _ = text ?? throw new ArgumentNullException(nameof(text));

    var embedding = await embeddingService.GenerateEmbeddingAsync(text);
    var kind = Enum.GetName(type) ?? string.Empty;
    var timeStamp = DateTime.UtcNow.ToString("o");

    var interaction = new UserData
    {
        Kind = kind,
        Text = text,
        Embedding = embedding,
        TimeStamp = timeStamp,
    };

    Console.WriteLine($"Persisting {type} data: {text}");

    await userCollection.UpsertAsync(interaction);
}

async Task<List<NewsArticle>> FetchNewsArticlesAsync(bool includeEmbedding = false, bool filter = true)
{
    var newsSources = new Dictionary<string, string>
    {
        ["CNN Top Stories"] = "http://rss.cnn.com/rss/cnn_topstories.rss",
        ["BBC Top Stories"] = "https://feeds.bbci.co.uk/news/rss.xml",
        ["NY Times Top Stories"] = "https://rss.nytimes.com/services/xml/rss/nyt/HomePage.xml",
    };

    var articles = new List<NewsArticle>();

    foreach (var feedUrl in newsSources)
    {
        var feed = await FeedReader.ReadAsync(feedUrl.Value);
        foreach (var item in feed.Items.Take(10))
        {
            var existingArticle = await GetNewsArticleAsync(item.Link);
            if (existingArticle != null)
            {
                continue;
            }

            var article = new NewsArticle
            {
                Title = item.Title,
                Content = item.Title, // update this to include the full content if available
                Url = item.Link,
            };

            if (includeEmbedding)
            {
                var newsEmbedding = await embeddingService.GenerateEmbeddingAsync(article.Content);
                article.Embedding = newsEmbedding;
            }

            articles.Add(article);
            await newsCollection.UpsertAsync(article);

            Console.WriteLine($"Fetched news article from {item.Link}");
        }
    }

    //if (filter)
    //{
    //    var includes = await userCollection.GetBatchAsync(("Kind = \"include\"");
    //    var excludes = await userCollection.GetDataAsync("Kind = \"exclude\"");

    //    result = [.. result
    //        .Where(n =>
    //            (includes.Length == 0
    //            || includes.Any(inc => n.Title.Contains(inc.Text, StringComparison.OrdinalIgnoreCase)))
    //            && !excludes.Any(exc => n.Title.Contains(exc.Text, StringComparison.OrdinalIgnoreCase)))];
    //}

    return articles;
}

async Task<NewsArticle?> GetNewsArticleAsync(string url)
{
    var searchString = "Get news article";
    var searchVector = await embeddingService.GenerateEmbeddingAsync(searchString);

    var vectorSearchOptions = new VectorSearchOptions<NewsArticle>
    {
        Top = 1,
        Filter = x =>
            x.Url == url,
    };

    var newsArticles =
        await newsCollection.VectorizedSearchAsync(
            vector: searchVector,
            options: vectorSearchOptions);

    var searchResult = await newsArticles.Results.FirstOrDefaultAsync();

    return searchResult?.Record;
}

void PrintNewsArticle(VectorSearchResult<NewsArticle> item)
{
    if (item != null && item.Score > 0.8)
    {
        Console.WriteLine($"{item.Record.Title}\n[{item.Record.Url}]\n(score {item.Score:F2})\n");
    }
}

void PrintUserData(VectorSearchResult<UserData> item)
{
    if (item != null && item.Score > 0.8)
    {
        var timeStamp = DateTime.Parse(item.Record.TimeStamp);
        var type = Enum.TryParse(item.Record.Kind, out UserDataType kind)
                ? kind
                : throw new ArgumentException($"Invalid UserDataType: {item.Record.Kind}");
        Console.WriteLine($"{item.Record.Text}\n[{type}]\n(score {item.Score:F2})\n");
    }
}
