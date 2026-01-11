namespace Flex.Infrastructures.Random
{
    public sealed class PasswordGenerationOptions
    {
        public int Length { get; init; } = 12;

        public bool RequireUppercase { get; init; } = true;
        public bool RequireLowercase { get; init; } = true;
        public bool RequireDigit { get; init; } = true;
        public bool RequireSpecial { get; init; } = true;

        public string SpecialCharacters { get; init; } = "!@#$%^&*()_+-=";
    }
}
