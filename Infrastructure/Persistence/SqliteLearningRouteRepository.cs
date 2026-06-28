using Microsoft.Data.Sqlite;
using Pathfinder.Application.Interfaces;
using Pathfinder.Domain.Entities;
using System.IO;

namespace Pathfinder.Infrastructure.Persistence;

public sealed class SqliteLearningRouteRepository : ILearningRouteRepository
{
    private readonly string _connectionString;

    public SqliteLearningRouteRepository()
    {
        var appFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Pathfinder");

        Directory.CreateDirectory(appFolder);
        DatabasePath = Path.Combine(appFolder, "pathfinder.db");
        _connectionString = new SqliteConnectionStringBuilder { DataSource = DatabasePath }.ToString();
    }

    public string DatabasePath { get; }

    public void Initialize()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS routes (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                title TEXT NOT NULL,
                overview TEXT NOT NULL,
                audience TEXT NOT NULL,
                level TEXT NOT NULL,
                created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
            );

            CREATE TABLE IF NOT EXISTS modules (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                route_id INTEGER NOT NULL,
                title TEXT NOT NULL,
                sort_order INTEGER NOT NULL,
                FOREIGN KEY(route_id) REFERENCES routes(id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS sources (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                module_id INTEGER NOT NULL,
                title TEXT NOT NULL,
                location TEXT NOT NULL,
                kind TEXT NOT NULL,
                is_completed INTEGER NOT NULL DEFAULT 0,
                sort_order INTEGER NOT NULL,
                FOREIGN KEY(module_id) REFERENCES modules(id) ON DELETE CASCADE
            );
            """;

        command.ExecuteNonQuery();
        SeedIfEmpty();
    }

    public List<LearningRoute> GetAll()
    {
        var routes = new List<LearningRoute>();

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, title, overview, audience, level FROM routes ORDER BY id DESC;";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            routes.Add(new LearningRoute
            {
                Id = reader.GetInt32(0),
                Title = reader.GetString(1),
                Overview = reader.GetString(2),
                Audience = reader.GetString(3),
                Level = reader.GetString(4)
            });
        }

        foreach (var route in routes)
        {
            route.Modules = GetModules(route.Id);
        }

        return routes;
    }

    public LearningRoute? GetById(int routeId)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, title, overview, audience, level FROM routes WHERE id = $id;";
        command.Parameters.AddWithValue("$id", routeId);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        var route = new LearningRoute
        {
            Id = reader.GetInt32(0),
            Title = reader.GetString(1),
            Overview = reader.GetString(2),
            Audience = reader.GetString(3),
            Level = reader.GetString(4)
        };

        route.Modules = GetModules(route.Id);
        return route;
    }

    public int Save(LearningRoute route)
    {
        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();

        if (route.Id == 0)
        {
            using var insertRoute = connection.CreateCommand();
            insertRoute.Transaction = transaction;
            insertRoute.CommandText = """
                INSERT INTO routes (title, overview, audience, level)
                VALUES ($title, $overview, $audience, $level);
                SELECT last_insert_rowid();
                """;
            AddRouteParameters(insertRoute, route);
            route.Id = Convert.ToInt32((long)insertRoute.ExecuteScalar()!);
        }
        else
        {
            using var updateRoute = connection.CreateCommand();
            updateRoute.Transaction = transaction;
            updateRoute.CommandText = """
                UPDATE routes
                SET title = $title, overview = $overview, audience = $audience, level = $level
                WHERE id = $id;
                """;
            updateRoute.Parameters.AddWithValue("$id", route.Id);
            AddRouteParameters(updateRoute, route);
            updateRoute.ExecuteNonQuery();
        }

        var existingModuleIds = GetModuleIds(connection, transaction, route.Id);

        for (var moduleIndex = 0; moduleIndex < route.Modules.Count; moduleIndex++)
        {
            var module = route.Modules[moduleIndex];
            module.RouteId = route.Id;
            module.SortOrder = moduleIndex;

            if (module.Id == 0)
            {
                using var insertModule = connection.CreateCommand();
                insertModule.Transaction = transaction;
                insertModule.CommandText = """
                    INSERT INTO modules (route_id, title, sort_order)
                    VALUES ($routeId, $title, $sortOrder);
                    SELECT last_insert_rowid();
                    """;
                insertModule.Parameters.AddWithValue("$routeId", module.RouteId);
                insertModule.Parameters.AddWithValue("$title", module.Title.Trim());
                insertModule.Parameters.AddWithValue("$sortOrder", module.SortOrder);
                module.Id = Convert.ToInt32((long)insertModule.ExecuteScalar()!);
            }
            else
            {
                using var updateModule = connection.CreateCommand();
                updateModule.Transaction = transaction;
                updateModule.CommandText = """
                    UPDATE modules
                    SET title = $title, sort_order = $sortOrder
                    WHERE id = $id;
                    """;
                updateModule.Parameters.AddWithValue("$id", module.Id);
                updateModule.Parameters.AddWithValue("$title", module.Title.Trim());
                updateModule.Parameters.AddWithValue("$sortOrder", module.SortOrder);
                updateModule.ExecuteNonQuery();
            }

            existingModuleIds.Remove(module.Id);
            SyncSources(connection, transaction, module);
        }

        foreach (var deletedModuleId in existingModuleIds)
        {
            using var deleteModule = connection.CreateCommand();
            deleteModule.Transaction = transaction;
            deleteModule.CommandText = "DELETE FROM modules WHERE id = $id;";
            deleteModule.Parameters.AddWithValue("$id", deletedModuleId);
            deleteModule.ExecuteNonQuery();
        }

        transaction.Commit();
        return route.Id;
    }

    public void Delete(int routeId)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM routes WHERE id = $id;";
        command.Parameters.AddWithValue("$id", routeId);
        command.ExecuteNonQuery();
    }

    public void ToggleSourceCompletion(int sourceId, bool isCompleted)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE sources SET is_completed = $isCompleted WHERE id = $id;";
        command.Parameters.AddWithValue("$id", sourceId);
        command.Parameters.AddWithValue("$isCompleted", isCompleted ? 1 : 0);
        command.ExecuteNonQuery();
    }

    private void SyncSources(SqliteConnection connection, SqliteTransaction transaction, LearningModule module)
    {
        var existingSourceIds = GetSourceIds(connection, transaction, module.Id);

        for (var sourceIndex = 0; sourceIndex < module.Sources.Count; sourceIndex++)
        {
            var source = module.Sources[sourceIndex];
            source.ModuleId = module.Id;
            source.SortOrder = sourceIndex;

            if (source.Id == 0)
            {
                using var insertSource = connection.CreateCommand();
                insertSource.Transaction = transaction;
                insertSource.CommandText = """
                    INSERT INTO sources (module_id, title, location, kind, is_completed, sort_order)
                    VALUES ($moduleId, $title, $location, $kind, $isCompleted, $sortOrder);
                    SELECT last_insert_rowid();
                    """;
                AddSourceParameters(insertSource, source);
                source.Id = Convert.ToInt32((long)insertSource.ExecuteScalar()!);
            }
            else
            {
                using var updateSource = connection.CreateCommand();
                updateSource.Transaction = transaction;
                updateSource.CommandText = """
                    UPDATE sources
                    SET title = $title,
                        location = $location,
                        kind = $kind,
                        is_completed = $isCompleted,
                        sort_order = $sortOrder
                    WHERE id = $id;
                    """;
                updateSource.Parameters.AddWithValue("$id", source.Id);
                AddSourceParameters(updateSource, source);
                updateSource.ExecuteNonQuery();
            }

            existingSourceIds.Remove(source.Id);
        }

        foreach (var deletedSourceId in existingSourceIds)
        {
            using var deleteSource = connection.CreateCommand();
            deleteSource.Transaction = transaction;
            deleteSource.CommandText = "DELETE FROM sources WHERE id = $id;";
            deleteSource.Parameters.AddWithValue("$id", deletedSourceId);
            deleteSource.ExecuteNonQuery();
        }
    }

    private List<LearningModule> GetModules(int routeId)
    {
        var modules = new List<LearningModule>();

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, route_id, title, sort_order
            FROM modules
            WHERE route_id = $routeId
            ORDER BY sort_order, id;
            """;
        command.Parameters.AddWithValue("$routeId", routeId);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            modules.Add(new LearningModule
            {
                Id = reader.GetInt32(0),
                RouteId = reader.GetInt32(1),
                Title = reader.GetString(2),
                SortOrder = reader.GetInt32(3)
            });
        }

        foreach (var module in modules)
        {
            module.Sources = GetSources(module.Id);
        }

        return modules;
    }

    private List<LearningSource> GetSources(int moduleId)
    {
        var sources = new List<LearningSource>();

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, module_id, title, location, kind, is_completed, sort_order
            FROM sources
            WHERE module_id = $moduleId
            ORDER BY sort_order, id;
            """;
        command.Parameters.AddWithValue("$moduleId", moduleId);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            sources.Add(new LearningSource
            {
                Id = reader.GetInt32(0),
                ModuleId = reader.GetInt32(1),
                Title = reader.GetString(2),
                Location = reader.GetString(3),
                Kind = Enum.TryParse<SourceKind>(reader.GetString(4), out var kind) ? kind : SourceKind.Link,
                IsCompleted = reader.GetInt32(5) == 1,
                SortOrder = reader.GetInt32(6)
            });
        }

        return sources;
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys = ON;";
        command.ExecuteNonQuery();

        return connection;
    }

    private static void AddRouteParameters(SqliteCommand command, LearningRoute route)
    {
        command.Parameters.AddWithValue("$title", route.Title.Trim());
        command.Parameters.AddWithValue("$overview", route.Overview.Trim());
        command.Parameters.AddWithValue("$audience", route.Audience.Trim());
        command.Parameters.AddWithValue("$level", route.Level.Trim());
    }

    private static void AddSourceParameters(SqliteCommand command, LearningSource source)
    {
        command.Parameters.AddWithValue("$moduleId", source.ModuleId);
        command.Parameters.AddWithValue("$title", source.Title.Trim());
        command.Parameters.AddWithValue("$location", source.Location.Trim());
        command.Parameters.AddWithValue("$kind", source.Kind.ToString());
        command.Parameters.AddWithValue("$isCompleted", source.IsCompleted ? 1 : 0);
        command.Parameters.AddWithValue("$sortOrder", source.SortOrder);
    }

    private static HashSet<int> GetModuleIds(SqliteConnection connection, SqliteTransaction transaction, int routeId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT id FROM modules WHERE route_id = $routeId;";
        command.Parameters.AddWithValue("$routeId", routeId);

        using var reader = command.ExecuteReader();
        var ids = new HashSet<int>();
        while (reader.Read())
        {
            ids.Add(reader.GetInt32(0));
        }

        return ids;
    }

    private static HashSet<int> GetSourceIds(SqliteConnection connection, SqliteTransaction transaction, int moduleId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT id FROM sources WHERE module_id = $moduleId;";
        command.Parameters.AddWithValue("$moduleId", moduleId);

        using var reader = command.ExecuteReader();
        var ids = new HashSet<int>();
        while (reader.Read())
        {
            ids.Add(reader.GetInt32(0));
        }

        return ids;
    }

    private void SeedIfEmpty()
    {
        if (GetAll().Count > 0)
        {
            return;
        }

        Save(new LearningRoute
        {
            Title = "JavaScript Fundamentals",
            Overview = "Master modern JavaScript features, async patterns, and functional programming concepts for industrial scale applications.",
            Audience = "Juniors",
            Level = "Beginners",
            Modules =
            [
                new LearningModule
                {
                    Title = "ES6+ Syntax",
                    Sources =
                    [
                        new LearningSource
                        {
                            Title = "JS ES6",
                            Kind = SourceKind.Video,
                            Location = "https://developer.mozilla.org/en-US/docs/Web/JavaScript",
                            IsCompleted = true
                        },
                        new LearningSource
                        {
                            Title = "Microsoft JS",
                            Kind = SourceKind.Docs,
                            Location = "https://learn.microsoft.com/en-us/training/paths/web-development-101/"
                        }
                    ]
                },
                new LearningModule
                {
                    Title = "Asynchronous JS",
                    Sources =
                    [
                        new LearningSource
                        {
                            Title = "Asincrono HJS",
                            Kind = SourceKind.Video,
                            Location = "https://developer.mozilla.org/en-US/docs/Learn/JavaScript/Asynchronous"
                        }
                    ]
                }
            ]
        });
    }
}
