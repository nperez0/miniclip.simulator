namespace Miniclip.Core.ReadModels.Projections.Attributes;

// Define a priority attribute
[AttributeUsage(AttributeTargets.Class)]
public class HandlerHighPriorityAttribute(int priority) : Attribute
{
    public int Priority { get; } = priority;
}
