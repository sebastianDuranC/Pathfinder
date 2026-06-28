namespace Pathfinder.Domain.Entities;

public sealed class LearningRoute
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Overview { get; set; } = "";
    public string Audience { get; set; } = "";
    public string Level { get; set; } = "Beginners";
    public List<LearningModule> Modules { get; set; } = [];

    public int SourceCount => Modules.Sum(module => module.Sources.Count);
    public int CompletedCount => Modules.Sum(module => module.Sources.Count(source => source.IsCompleted));
    public int Progress => SourceCount == 0 ? 0 : (int)Math.Round(CompletedCount * 100.0 / SourceCount);
}
