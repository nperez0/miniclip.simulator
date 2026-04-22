namespace Miniclip.Core.ReadModels.Projections.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class HandlerHighPriorityAttribute(int priority) : Attribute
{
    public int Priority { get; } = priority;
}
