namespace Miniclip.Core.Messaging;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
public sealed class MessageTypeAttribute(string value) : Attribute
{
    public string Value { get; } = value;
}
