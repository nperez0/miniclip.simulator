namespace Miniclip.Simulator.Application.Commands.Groups.V1.Generation;

public record GenerateGroupRequest(string Name, int Capacity)
{
    public GenerateGroupCommand ToCommand() => new(Name, Capacity);
}
