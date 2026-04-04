using System.Text.RegularExpressions;
using Miniclip.Core.Domain;

namespace Miniclip.Core.Kafka;

public static partial class TopicNaming
{
    private static readonly Regex PascalCasePattern = PascalRegex();

    public static string ForAggregate<TAggregate>() where TAggregate : AggregateRoot
        => ForAggregate(typeof(TAggregate).Name);

    public static string ForAggregate(string aggregateType)
        => $"simulator.{PascalCasePattern.Replace(aggregateType, "-$1").ToLowerInvariant()}";

    [GeneratedRegex("(?<=.)([A-Z])", RegexOptions.Compiled)]
    private static partial Regex PascalRegex();
}
