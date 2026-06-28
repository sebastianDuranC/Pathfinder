# Pathfinder

App WPF de escritorio para crear rutas de aprendizaje, organizar modulos y fuentes, y guardar el progreso localmente con SQLite.

## Arquitectura N capas

```text
Pathfinder/
├── Domain/
│   └── Entities/              # Entidades puras del negocio
├── Application/
│   ├── Interfaces/            # Contratos que la app necesita
│   └── Services/              # Casos de uso y validaciones
├── Infrastructure/
│   └── Persistence/           # SQLite y acceso a datos
└── MainWindow.xaml(.cs)       # Presentacion WPF
```

## Reglas de dependencia

- `Domain` no depende de ninguna otra capa.
- `Application` depende de `Domain` y define contratos como `ILearningRouteRepository`.
- `Infrastructure` implementa los contratos de `Application` usando SQLite.
- `Presentation` crea la UI WPF y llama a `LearningRouteService`, no a SQLite directamente.

## Base de datos

La base SQLite se crea automaticamente en:

```text
%LOCALAPPDATA%\Pathfinder\pathfinder.db
```

## Ejecutar

```powershell
dotnet run
```
