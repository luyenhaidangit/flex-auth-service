using System.Security.Cryptography;

namespace Flex.Infrastructures.Random
{
    public static class RandomPasswordGenerator
    {
        private const string Uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string Lowercase = "abcdefghijklmnopqrstuvwxyz";
        private const string Digits = "0123456789";

        public static string Generate(PasswordGenerationOptions options)
        {
            if (options.Length < 6)
                throw new ArgumentException("Password length must be at least 6");

            var pools = new List<string>();
            var requiredChars = new List<char>();

            if (options.RequireUppercase)
            {
                pools.Add(Uppercase);
                requiredChars.Add(GetRandomChar(Uppercase));
            }

            if (options.RequireLowercase)
            {
                pools.Add(Lowercase);
                requiredChars.Add(GetRandomChar(Lowercase));
            }

            if (options.RequireDigit)
            {
                pools.Add(Digits);
                requiredChars.Add(GetRandomChar(Digits));
            }

            if (options.RequireSpecial)
            {
                pools.Add(options.SpecialCharacters);
                requiredChars.Add(GetRandomChar(options.SpecialCharacters));
            }

            var allChars = string.Concat(pools);
            var result = new List<char>(requiredChars);

            while (result.Count < options.Length)
            {
                result.Add(GetRandomChar(allChars));
            }

            Shuffle(result);
            return new string(result.ToArray());
        }

        private static char GetRandomChar(string chars)
        {
            var index = RandomNumberGenerator.GetInt32(chars.Length);
            return chars[index];
        }

        private static void Shuffle(IList<char> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = RandomNumberGenerator.GetInt32(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
