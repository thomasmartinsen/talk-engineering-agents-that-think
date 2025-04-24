using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace Agents.Tools;

public class NewsPlugin
{
    [KernelFunction("SummarizeNews"), Description("Function to get and summarize news articles from various news sources.")]
    public string SummarizeNews()
    {
        return $"Hello World";
    }
}