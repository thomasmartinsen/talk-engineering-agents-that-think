using System.Security.Cryptography;
using System.Text;

namespace Agents;

public class ModelFactory<T>
{
    private static T _currentKey;

    static ModelFactory()
    {
        _currentKey = default(T);
    }

    public static List<AgentDataModel<T>> GetData()
    {
        List<AgentDataModel<T>> data =
        [
            new() {
                id = KeyGeneratorReturnNext(),
                Name = "Peter",
                Tags = new[] { "peter", "senior", "developer" },
                Description = "Senior developer working on multiple projects.",
            },
            new() {
                id = KeyGeneratorReturnNext(),
                Name = "Hanne",
                Tags = new[] { "hanne", "project manager" },
                Description = "Project manager working on 1-2 projects.",
            },
            new() {
                id = KeyGeneratorReturnNext(),
                Name = "Kim",
                Tags = new[] { "kim", "architect" },
                Description = "Architect working on the biggest projects.",
            }
        ];

        return data;
    }

    public static List<AgentDataModelVector<T>> GetVectorDataAsync()
    {
        var data = GetData();
        var vectorData = new List<AgentDataModelVector<T>>();

        foreach (var item in data)
        {
            vectorData.Add(new AgentDataModelVector<T>
            {
                id = item.id,
                Name = item.Name,
                Description = item.Description
            });
        }

        return vectorData;
    }

    public static T KeyGeneratorReturnNext()
    {
        _currentKey = typeof(T) switch
        {
            Type t when t == typeof(int) => (T)Convert.ChangeType(Convert.ToInt32(_currentKey) + 1, typeof(T)),
            Type t when t == typeof(long) => (T)Convert.ChangeType(Convert.ToInt64(_currentKey) + 1, typeof(T)),
            Type t when t == typeof(ulong) => (T)Convert.ChangeType(Convert.ToUInt64(_currentKey) + 1, typeof(T)),
            Type t when t == typeof(string) => (T)Convert.ChangeType(Guid.NewGuid().ToString(), typeof(T)),
            _ => throw new InvalidOperationException("Unsupported key type")
        };

        return _currentKey;
    }

    public static List<AgentDataModelVector<T>> GetSemanticKernelSampleData()
    {
        List<string> documentation =
       [
           "https://raw.githubusercontent.com/microsoft/kernel-memory/main/README.md",
            "https://microsoft.github.io/kernel-memory/quickstart",
            "https://microsoft.github.io/kernel-memory/quickstart/configuration",
            "https://microsoft.github.io/kernel-memory/quickstart/start-service",
            "https://microsoft.github.io/kernel-memory/quickstart/python",
            "https://microsoft.github.io/kernel-memory/quickstart/csharp",
            "https://microsoft.github.io/kernel-memory/quickstart/java",
            "https://microsoft.github.io/kernel-memory/quickstart/javascript",
            "https://microsoft.github.io/kernel-memory/quickstart/bash",
            "https://microsoft.github.io/kernel-memory/service",
            "https://microsoft.github.io/kernel-memory/service/architecture",
            "https://microsoft.github.io/kernel-memory/serverless",
            "https://microsoft.github.io/kernel-memory/security/filters",
            "https://microsoft.github.io/kernel-memory/how-to/custom-partitioning",
            "https://microsoft.github.io/kernel-memory/concepts/indexes",
            "https://microsoft.github.io/kernel-memory/concepts/document",
            "https://microsoft.github.io/kernel-memory/concepts/memory",
            "https://microsoft.github.io/kernel-memory/concepts/tag",
            "https://microsoft.github.io/kernel-memory/concepts/llm",
            "https://microsoft.github.io/kernel-memory/concepts/embedding",
            "https://microsoft.github.io/kernel-memory/concepts/cosine-similarity",
            "https://microsoft.github.io/kernel-memory/faq",
            "https://raw.githubusercontent.com/microsoft/semantic-kernel/main/README.md",
            "https://raw.githubusercontent.com/microsoft/semantic-kernel/main/dotnet/README.md",
            "https://raw.githubusercontent.com/microsoft/semantic-kernel/main/python/README.md",
            "https://raw.githubusercontent.com/microsoft/semantic-kernel/main/java/README.md",
            "https://learn.microsoft.com/en-us/semantic-kernel/overview/",
            "https://learn.microsoft.com/en-us/semantic-kernel/get-started/quick-start-guide",
            "https://learn.microsoft.com/en-us/semantic-kernel/agents/",
        ];

        List<AgentDataModelVector<T>> data = new();

        foreach (var item in documentation)
        {
            var id = GetUrlId(item);
            data.Add(new AgentDataModelVector<T>
            {
                id = KeyGeneratorReturnNext(),
                Name = item,
                Tags = new[] { item, id },
                Description = id,
            });
        }

        return data;
    }

    private static string GetUrlId(string url)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(url))).ToUpperInvariant();
    }
}
