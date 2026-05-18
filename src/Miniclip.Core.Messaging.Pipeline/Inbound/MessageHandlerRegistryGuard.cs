namespace Miniclip.Core.Messaging.Pipeline.Inbound;

internal static class MessageHandlerRegistryGuard
{
    internal static void Validate(IEnumerable<CompiledMessageHandler> handlers)
    {
        var seen = new Dictionary<string, Type>(StringComparer.Ordinal);
        var duplicates = new List<string>();

        foreach (var handler in handlers)
        {
            var key = handler.MessageType.GetMessageTypeName();

            if (!seen.TryAdd(key, handler.MessageType))
            {
                duplicates.Add(
                    $"'{key}' claimed by both '{seen[key].FullName}' and '{handler.MessageType.FullName}'");
            }
        }

        if (duplicates.Count > 0)
            throw new InvalidOperationException(
                "Duplicate message type names detected in handler registry. " +
                "Each message type must resolve to a unique name via [MessageType] or FullName.\n" +
                string.Join('\n', duplicates));
    }
}
