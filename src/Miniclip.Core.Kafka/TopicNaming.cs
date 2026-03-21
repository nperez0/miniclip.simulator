using System.Text.RegularExpressions;
using Miniclip.Core.Domain;

namespace Miniclip.Core.Kafka;

public static class TopicNaming
{
    private static readonly Regex PascalCasePattern = new(@"(?<=.)([A-Z])", RegexOptions.Compiled);

    public static string For(IDomainEvent @event)
        => $"simulator.{PascalCasePattern.Replace(@event.GetType().Name, "-$1").ToLowerInvariant()}";
}
