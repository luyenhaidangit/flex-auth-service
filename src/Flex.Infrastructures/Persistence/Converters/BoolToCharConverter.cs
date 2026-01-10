using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Flex.Infrastructures.Persistence.Converters
{
    public sealed class BoolToCharConverter : ValueConverter<bool, string>
    {
        public BoolToCharConverter()
            : base(
                v => v ? "Y" : "N",
                v => v == "Y"
            )
        { }
    }
}
