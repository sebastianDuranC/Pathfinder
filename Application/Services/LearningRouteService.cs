using Pathfinder.Application.Interfaces;
using Pathfinder.Domain.Entities;

namespace Pathfinder.Application.Services;

public sealed class LearningRouteService
{
    private readonly ILearningRouteRepository _repository;

    public LearningRouteService(ILearningRouteRepository repository)
    {
        _repository = repository;
    }

    public string DatabasePath => _repository.DatabasePath;

    public void Initialize() => _repository.Initialize();

    public List<LearningRoute> GetRoutes() => _repository.GetAll();

    public LearningRoute? GetRoute(int routeId) => _repository.GetById(routeId);

    public int SaveRoute(LearningRoute route)
    {
        ValidateRoute(route);
        return _repository.Save(route);
    }

    public void DeleteRoute(int routeId) => _repository.Delete(routeId);

    public void ToggleSourceCompletion(int sourceId, bool isCompleted) =>
        _repository.ToggleSourceCompletion(sourceId, isCompleted);

    private static void ValidateRoute(LearningRoute route)
    {
        if (string.IsNullOrWhiteSpace(route.Title))
        {
            throw new InvalidOperationException("La ruta necesita un titulo.");
        }

        if (route.Modules.Count == 0)
        {
            throw new InvalidOperationException("Agrega al menos un modulo.");
        }

        foreach (var module in route.Modules)
        {
            if (string.IsNullOrWhiteSpace(module.Title))
            {
                throw new InvalidOperationException("Todos los modulos necesitan titulo.");
            }

            foreach (var source in module.Sources)
            {
                if (string.IsNullOrWhiteSpace(source.Title))
                {
                    throw new InvalidOperationException("Todas las fuentes necesitan titulo.");
                }
            }
        }
    }
}
