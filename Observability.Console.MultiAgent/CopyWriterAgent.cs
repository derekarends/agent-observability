using Microsoft.SemanticKernel;

namespace Observability.Console.MultiAgent;

public class DirectorAgent : Agent
{
    private const string DirectorName = "ArtDirector";
    private const string DirectorInstructions = """
        You are an art director who has opinions about copywriting born of a love for David Ogilvy.
        The goal is to determine if the given copy is acceptable to print.
        You must make sure the copy is legally sound before approving.
        If it is not legal be very clear on why it is not, and how to fix it.
        If so, state that it is approved.
        If not, provide insight on how to refine suggested copy without example.
        """;


    public DirectorAgent(Kernel kernel) : base(kernel)
    {
        ServiceId = "DirectorAgent";
        Name = DirectorName;
        Instructions = DirectorInstructions;
    }
}
