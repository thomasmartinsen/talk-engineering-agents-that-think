using Agents;
using Microsoft.SemanticKernel;

var openAIApiKey = Configuration.GetValue("OPENAI_APIKEY");
var openAIModel = Configuration.GetValue("OPENAI_MODELID", "gpt-4");

var azureOpenAIEndpoint = Configuration.GetValue("AZURE_OPENAI_ENDPOINT");
var azureOpenAIApiKey = Configuration.GetValue("AZURE_OPENAI_APIKEY");
var azureOpenAIChatModel = Configuration.GetValue("AZURE_OPENAI_CHAT_MODELID");
var azureOpenAIEmbeddedModel = Configuration.GetValue("AZURE_OPENAI_EMBEDDED_MODELID");

var cosmosDbConnectionString = Configuration.GetValue("AZURE_COSMOSDB_CONNECTIONSTRING");
var cosmosDbName = "AgentDB";
var collectionName = "memory";

Kernel kernel = Kernel.CreateBuilder()
    .AddAzureOpenAIChatCompletion(azureOpenAIChatModel, azureOpenAIEndpoint, azureOpenAIApiKey)
    .Build();
