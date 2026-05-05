using System.Reflection;

namespace Miniclip.Core.Messaging;

public static class MessageTypeAttributeExtensions
{
    public static string GetMessageTypeName(this Type type)
        => type.GetCustomAttribute<MessageTypeAttribute>()?.Value ?? type.FullName!;
}
