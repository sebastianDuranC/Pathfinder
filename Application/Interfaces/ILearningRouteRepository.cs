using Pathfinder.Domain.Entities;

namespace Pathfinder.Application.Interfaces;

public interface ILearningRouteRepository
{
    string DatabasePath { get; }
    void Initialize();
    List<LearningRoute> GetAll();
    LearningRoute? GetById(int routeId);
    int Save(LearningRoute route);
    void Delete(int routeId);
    void ToggleSourceCompletion(int sourceId, bool isCompleted);
}
