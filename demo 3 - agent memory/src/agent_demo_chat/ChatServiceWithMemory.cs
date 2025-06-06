﻿using Agents.Models;
using CodeHollow.FeedReader;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Data;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;

namespace Agents;

internal sealed class ChatServiceWithMemory<TKey>(
    UniqueKeyGenerator<TKey> uniqueKeyGenerator,
    IVectorStoreRecordCollection<TKey, TextSnippet<TKey>> newsCollection,
    IVectorStoreRecordCollection<TKey, UserQuery> userQueryCollection,
    ITextEmbeddingGenerationService embeddingService,
    IChatCompletionService chatCompletionService,
    VectorStoreTextSearch<TextSnippet<TKey>> vectorStoreTextSearch,
    Kernel kernel,
    [FromKeyedServices("AppShutdown")] CancellationTokenSource appShutdownCancellationTokenSource) : IHostedService
{
    private Task? _dataLoaded;
    private Task? _chatLoop;

    /// <summary>
    /// Start the service.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>An async task that completes when the service is started.</returns>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _dataLoaded = LoadDataAsync(cancellationToken);
        _chatLoop = ChatLoopAsync(cancellationToken);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Stop the service.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>An async task that completes when the service is stopped.</returns>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Contains the main chat loop for the application.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>An async task that completes when the chat loop is shut down.</returns>
    private async Task ChatLoopAsync(CancellationToken cancellationToken)
    {
        // Wait for the data to be loaded before starting the chat loop.
        while (_dataLoaded != null && !_dataLoaded.IsCompleted && !cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(1_000, cancellationToken).ConfigureAwait(false);
        }

        if (_dataLoaded != null && _dataLoaded.IsFaulted)
        {
            Console.WriteLine("Failed to load data");
            return;
        }

        Console.Clear();
        Console.WriteLine("News data loading complete\n");

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Agent: Press enter with no prompt to exit.");

        // Add a search plugin to the kernel which we will use in the template below
        // to do a vector search for related information to the user query.
        kernel.Plugins.Add(vectorStoreTextSearch.CreateWithGetTextSearchResults("SearchPlugin"));

        // Start the chat loop.
        while (!cancellationToken.IsCancellationRequested)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Agent: What would you like to know?");

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("User: ");
            var question = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(question))
            {
                appShutdownCancellationTokenSource.Cancel();
                break;
            }

            await PersistUserQueryAsync(question, cancellationToken);

            var response = kernel.InvokePromptStreamingAsync(
                promptTemplate: """
                    Please use this information to answer the question:
                    {{#with (SearchPlugin-GetTextSearchResults question)}}  
                      {{#each this}}  
                        Name: {{Name}}
                        Value: {{Value}}
                        Link: {{Link}}
                        -----------------
                      {{/each}}
                    {{/with}}

                    Include citations to the relevant information where it is referenced in the response.
                    
                    Question: {{question}}
                    """,
                arguments: new KernelArguments()
                {
                    { "question", question },
                },
                templateFormat: "handlebars",
                promptTemplateFactory: new HandlebarsPromptTemplateFactory(),
                cancellationToken: cancellationToken);

            try
            {
                await foreach (var message in response.ConfigureAwait(false))
                {
                    Console.Write(message);
                }

                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Call to LLM failed with error: {ex}");
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("\nAssistant: ");
        }
    }

    /// <summary>
    /// Load all data into the vector store.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>An async task that completes when the loading is complete.</returns>
    private async Task LoadDataAsync(CancellationToken cancellationToken)
    {
        try
        {
            await newsCollection.CreateCollectionIfNotExistsAsync(cancellationToken).ConfigureAwait(false);

            Console.WriteLine("Loading data...");

            var newsSources = new Dictionary<string, string>
            {
                [".NET blog"] = "https://devblogs.microsoft.com/dotnet/feed/",
                ["Semantic Kernel blog"] = "https://devblogs.microsoft.com/semantic-kernel/feed/",
                ["Azure AI Foundry blog"] = "https://devblogs.microsoft.com/foundry/feed/"
            };

            var articles = new List<TextSnippet<TKey>>();

            foreach (var feedUrl in newsSources)
            {
                var feed = await FeedReader.ReadAsync(feedUrl.Value);
                foreach (var item in feed.Items.Take(10))
                {
                    var existingArticle = await GetItemFromLinkAsync(item.Link);
                    if (existingArticle != null)
                    {
                        continue;
                    }

                    var article = new TextSnippet<TKey>
                    {
                        Key = uniqueKeyGenerator.GenerateKey(),
                        Text = item.Title,
                        ReferenceDescription = item.Description,
                        ReferenceLink = item.Link,
                    };

                    var newsEmbedding = await embeddingService.GenerateEmbeddingAsync(article.Text);
                    article.TextEmbedding = newsEmbedding;

                    articles.Add(article);
                    await newsCollection.UpsertAsync(article);

                    Console.WriteLine($"Fetched news article from {item.Link}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load news data: {ex}");
            throw;
        }
    }

    private async Task<TextSnippet<TKey>?> GetItemFromLinkAsync(string link)
    {
        var searchString = "Get item";
        var searchVector = await embeddingService.GenerateEmbeddingAsync(searchString);

        var vectorSearchOptions = new VectorSearchOptions<TextSnippet<TKey>>
        {
            Top = 1,
            Filter = x =>
                x.ReferenceLink == link,
        };

        var newsArticles =
            await newsCollection.VectorizedSearchAsync(
                vector: searchVector,
                options: vectorSearchOptions);

        var searchResult = await newsArticles.Results.FirstOrDefaultAsync();

        var result = searchResult?.Record;
        Console.WriteLine($"{result?.ReferenceLink}");

        return result;
    }

    private async Task PersistUserQueryAsync(string query, CancellationToken cancellationToken)
    {
        _ = query ?? throw new ArgumentNullException(nameof(query));

        await userQueryCollection.CreateCollectionIfNotExistsAsync(cancellationToken).ConfigureAwait(false);

        string summary = await GenerateSummaryAsync(query, cancellationToken) ?? "";
        var embedding = await embeddingService.GenerateEmbeddingAsync(query, cancellationToken: cancellationToken);

        var userQuery = new UserQuery
        {
            Query = query,
            Summary = summary,
            Embedding = embedding,
        };

        await userQueryCollection.UpsertAsync(userQuery, cancellationToken);
    }

    private async Task<string?> GenerateSummaryAsync(string query, CancellationToken cancellationToken)
    {
        try
        {
            var chatHistory = new ChatHistory();

            chatHistory.AddSystemMessage("You are a helpful assistant that summarizes user queries concisely in one sentence.");
            chatHistory.AddUserMessage($"Summarize the following query: {query}");

            var executionSettings = new AzureOpenAIPromptExecutionSettings
            {
                MaxTokens = 50,
                Temperature = 0.7
            };

            var response = await chatCompletionService.GetChatMessageContentAsync(
                chatHistory: chatHistory,
                executionSettings: executionSettings,
                cancellationToken: cancellationToken);

            _ = response ?? throw new Exception("No summary generated.");

            return response.Content?.Trim();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to generate summary: {ex.Message}");
            return "Summary generation failed.";
        }
    }
}