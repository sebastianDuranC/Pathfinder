# Pathfinder

App WPF de escritorio para crear rutas de aprendizaje, organizar modulos y fuentes, y guardar el progreso localmente con SQLite.

<table>
  <tr>
    <td valign="top" width="50%">
      <p align="center"><b>Vista del dashboard</b></p>
      <img src="https://github.com/user-attachments/assets/f656a90e-4836-4891-97c9-72d854690112" alt="Dashboard" style="max-width:100%;" />
    </td>
    <td valign="top" width="50%">
      <p align="center"><b>Creación de las rutas</b></p>
      <img src="https://github.com/user-attachments/assets/cbdb0bef-b9e8-4f26-b79a-76737b12697c" alt="Create" style="max-width:100%;" />
    </td>
  </tr>
</table>
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
