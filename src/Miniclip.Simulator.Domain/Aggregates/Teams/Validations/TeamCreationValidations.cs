using Miniclip.Simulator.Domain.Aggregates.Teams.Entities;

namespace Miniclip.Simulator.Domain.Aggregates.Teams.Validations;

public static class TeamCreationValidations
{
    extension(Validation<Team> validation)
    {
        public Validation<Team> HasValidName(string? name)
            => validation.Ensure(name.IsNotNullOrWhiteSpace(), $"Team name '{name}' cannot be empty.");

        public Validation<Team> HasValidStrength(int strength)
            => validation.Ensure(strength is >= 0 and <= 100, $"Strength '{strength}' must be between 0 and 100.");
    }
}
