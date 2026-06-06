namespace Grimoire.Application.Import;

public sealed class ImportStrategyFactory(IEnumerable<IImportStrategy> strategies)
{
    public IImportStrategy GetStrategy(string fileName)
    {
        foreach (var strategy in strategies)
        {
            if (strategy.CanHandle(fileName))
                return strategy;
        }

        throw new InvalidOperationException(
            $"No import strategy found for file: {fileName}");
    }
}
