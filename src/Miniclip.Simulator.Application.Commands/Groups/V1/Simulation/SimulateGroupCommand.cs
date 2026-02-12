using Miniclip.Core.Application;

namespace Miniclip.Simulator.Application.Commands.Groups.V1.Simulation;

public record SimulateGroupCommand(Guid GroupId) : ICommand;
