namespace Pathfinder.Domain.Entities;

public sealed class LearningModule
{
    public int Id { get; set; }
    public int RouteId { get; set; }
    public string Title { get; set; } = "";
    public int SortOrder { get; set; }
    public List<LearningSource> Sources { get; set; } = [];
}
