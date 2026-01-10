namespace Flex.Domain.Abstractions
{
    public abstract class EntityBase<TId>
    {
        public TId Id { get; protected set; } = default!;
    }
}
