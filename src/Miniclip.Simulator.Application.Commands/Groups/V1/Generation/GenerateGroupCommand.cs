using Miniclip.Core.Application;

namespace Miniclip.Simulator.Application.Commands.Groups.V1.Generation;

public record GenerateGroupCommand(string Name, int Capacity) : ICommand<Guid>;
