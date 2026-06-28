namespace Pathfinder.Domain.Entities;

public sealed class LearningSource
{
    public int Id { get; set; }
    public int ModuleId { get; set; }
    public string Title { get; set; } = "";
    public string Location { get; set; } = "";
    public SourceKind Kind { get; set; } = SourceKind.Link;
    public bool IsCompleted { get; set; }
    public int SortOrder { get; set; }
}
