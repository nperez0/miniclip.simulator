namespace Miniclip.Core.ReadModels.Projections.Attributes;

// Define a priority attribute
public class HandlerPriorityAttribute : Attribute
{
    public int Priority { get; }
    public HandlerPriorityAttribute(int priority) => Priority = priority;
}
