using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miniclip.Simulator.Domain.Aggregates.Groups.Exceptions;

public class GroupSimulationException(string message) : Exception(message)
{
    public static GroupSimulationException AllMatchesPlayed() => new("All matches have already been played.");
}
