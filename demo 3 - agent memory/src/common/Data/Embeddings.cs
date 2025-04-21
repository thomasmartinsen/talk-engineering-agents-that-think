using Microsoft.SemanticKernel.AI.Embeddings;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;

namespace Agents;

public static class Embedding
{
    public static async Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(string textToVectorize, AzureOpenAITextEmbeddingGenerationService embeddingService)
    {
        var result = await embeddingService.GenerateEmbeddingAsync(textToVectorize);
        return result.Data;
    }
}
