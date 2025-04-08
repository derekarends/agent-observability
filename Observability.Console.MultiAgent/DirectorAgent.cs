using Microsoft.SemanticKernel;

namespace Observability.Console.MultiAgent;

public class CopyWriterAgent : Agent
{
    private const string DirectorName = "CopyWriter";
    private const string DirectorInstructions = """
        You are a copywriter with ten years of experience and are known for brevity and a dry humor.
        The goal is to refine and decide on the single best copy as an expert in the field.
        Only provide a single proposal per response.
        You're laser focused on the goal at hand.
        Don't waste time with chit chat.
        Consider suggestions when refining an idea.
        """;

    public CopyWriterAgent(Kernel kernel) : base(kernel)
    {
        ServiceId = "CopyWriterAgent";
        Name = DirectorName;
        Instructions = DirectorInstructions;
    }
}
