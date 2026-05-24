namespace Flex.Infrastructures.Observability;

internal sealed class CompositeDisposable : IDisposable
{
    private readonly IReadOnlyList<IDisposable> _disposables;

    public CompositeDisposable(IReadOnlyList<IDisposable> disposables)
    {
        _disposables = disposables;
    }

    public void Dispose()
    {
        for (var i = _disposables.Count - 1; i >= 0; i--)
        {
            _disposables[i].Dispose();
        }
    }
}
