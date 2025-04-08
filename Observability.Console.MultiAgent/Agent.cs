using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.Connectors.OpenAI;

#pragma warning disable SKEXP0001, SKEXP0110

namespace Observability.Console.MultiAgent;

public abstract class Agent
{
    private readonly Kernel _kernel;

    public string ServiceId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;

    public Agent(Kernel kernel)
    {
        _kernel = kernel;
    }

    public ChatCompletionAgent Create()
    {
        var agent = new ChatCompletionAgent()
        {
            Name = Name,
            Instructions = Instructions,
            Kernel = _kernel,
            Arguments = new KernelArguments(
                 new OpenAIPromptExecutionSettings()
                 {
                     FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
                 })
        };

        return agent;
    }
}

public class ApprovalTerminationStrategy : TerminationStrategy
{
    protected override Task<bool> ShouldAgentTerminateAsync(Microsoft.SemanticKernel.Agents.Agent agent, IReadOnlyList<ChatMessageContent> history, CancellationToken cancellationToken)
    {
        var lastMessage = history.LastOrDefault();
        if (lastMessage != null && lastMessage.Content.ToLower().Contains("approved"))
        {
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }
}