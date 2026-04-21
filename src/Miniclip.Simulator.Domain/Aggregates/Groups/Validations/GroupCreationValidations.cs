using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;

namespace Miniclip.Simulator.Domain.Aggregates.Groups.Validations;

public static class GroupCreationValidations
{
    extension(Validation<Group> validation)
    {
        public Validation<Group> HasValidName(string? name)
            => validation.Ensure(name.IsNotNullOrWhiteSpace(), $"Group name '{name}' cannot be empty.");

        public Validation<Group> HasValidCapacity(int capacity, int min, int max)
            => validation.Ensure(capacity >= min && capacity <= max, $"Group capacity must be between {min} and {max}, but was {capacity}.");
    }
}
