# **Pathfinder**

#### Es una aplicación de escritorio moderna desarrollada en WPF y C# (.NET 10) diseñada para ayudarte a estructurar tus rutas de aprendizaje, te permite organizar módulos de estudio, arrastrar y soltar recursos, guardar enlaces web, videos, documentos o archivos locales, y registrar tu progreso en tiempo real de forma 100% local mediante SQLite.

## 📷 Vista Previa de la Aplicación
<table align="center">
  <tr>
    <td align="center" width="33%">
      <b>Dashboard</b><br/>
      <p>Panel principal para explorar y buscar tus rutas de aprendizaje.</p>
      <img width="1281" height="827" alt="Captura de pantalla 2026-06-28 184036" src="https://github.com/user-attachments/assets/bb4c2d1f-58ab-42e0-874a-e082b7a0666c" />
    </td>
    <td align="center" width="33%">
      <b>Ruta de Progreso</b><br/>
      <p>Detalle interactivo con línea de tiempo y contador de temas.</p>
      <img width="1279" height="833" alt="Captura de pantalla 2026-06-28 184216" src="https://github.com/user-attachments/assets/f685ed77-dd71-4539-b783-8de723cffc99" />
    </td>
    <td align="center" width="33%">
      <b>Creación y editor de Rutas</b><br/>
      <p>Creación y edición modular con soporte Drag & Drop de elementos.</p>
      <img width="1280" height="831" alt="Captura de pantalla 2026-06-28 184324" src="https://github.com/user-attachments/assets/85cb9f0e-0e9a-4450-9c02-64911fd9ee77" />
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
└── ....
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
## Crear un ejecutable .exe portable
Si deseas compilar la aplicación para generar un único archivo ejecutable .exe que incluya todo lo necesario (runtime de .NET, bases de datos y librerías nativas):
```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishReadyToRun=true -p:IncludeNativeLibrariesForSelfExtract=true
```
