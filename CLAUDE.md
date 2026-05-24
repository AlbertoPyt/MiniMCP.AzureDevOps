# Guía de trabajo para asistentes IA

Este fichero es leído automáticamente por Claude Code. Si usas otro asistente IA
(GitHub Copilot, Cursor, Windsurf, GPT, etc.), proporciónselo como contexto antes
de pedir cambios. Las reglas aquí son de obligado cumplimiento para cualquier IA
que trabaje en este repositorio.

---

## 1. Autoría de commits

- **Nunca** añadas trazas de co-autoría de IA en los mensajes de commit.
  No uses `Co-Authored-By`, ni menciones "Claude", "GPT", "Copilot" ni ningún
  asistente en el historial de Git.
- El autor de todos los commits es el desarrollador humano configurado en Git:
  `AlbertoPyt <albertopyt1407@gmail.com>`.
- El mensaje de commit debe redactarse como si lo hubiera escrito el desarrollador,
  en primera o tercera persona técnica, nunca en nombre de la IA.

---

## 2. Directivas `using` — siempre en `GlobalUsings.cs`

- **Nunca** añadas `using` al inicio de un fichero de clase `.cs` individual.
- Todos los `using` van en el fichero `GlobalUsings.cs` del proyecto correspondiente.
  Si no existe, créalo.
- Cada proyecto tiene su propio `GlobalUsings.cs` en la raíz del proyecto.
- No dupliques usings que ya añade el SDK implícitamente
  (`ImplicitUsings=enable` en el `.csproj`).
- Si al refactorizar un using de una clase resulta que era el único consumidor,
  añádelo igualmente a `GlobalUsings.cs` (consistencia antes que ahorro).

**Estructura de ejemplo:**
```
src/
  MCP.AzureDevOps.Domain/
    GlobalUsings.cs          ← usings del proyecto Domain
  MCP.AzureDevOps.Application/
    GlobalUsings.cs          ← usings del proyecto Application
  ...
```

---

## 3. Idioma

- **Código** (nombres de clases, métodos, variables, propiedades): inglés.
- **Comentarios XML/doc** (`///`), mensajes de log, mensajes de error al usuario
  y mensajes de commit: **español**.
- **Tests**: nombres de métodos en español descriptivo
  (`Register_SinPat_Returns400`, `Forward_CuentaInexistente_Returns401`).

---

## 4. Estilo general de código

- Usa **primary constructors** de C# cuando el tipo solo tiene inyección de
  dependencias (sin lógica adicional en el constructor).
- Usa **`sealed`** en clases que no están pensadas para herencia.
- Usa **`record`** para value objects y DTOs inmutables.
- No sobreescribas `ToString()` en value objects que contengan datos sensibles
  (tokens, contraseñas, claves).
- Prefiere **`IReadOnlyList<T>`** / **`IReadOnlyDictionary<K,V>`** en firmas
  públicas; usa `List<T>` / `Dictionary<K,V>` solo en implementaciones privadas.

---

## 5. Arquitectura

Este proyecto sigue **Clean Architecture / Hexagonal**. Las reglas de dependencia
son estrictas:

```
Domain  ←  Application  ←  Infrastructure
                        ←  Host
                        ←  Cli
                        ←  Sdk
```

- `Domain` no depende de ningún paquete NuGet externo.
- `Application` solo depende de `Domain` y de abstracciones (`IOptions<T>` está
  permitido, `DbContext` no).
- `Infrastructure` implementa los puertos definidos en `Application`.
- `Host`, `Cli` y `Sdk` son los puntos de entrada; pueden referenciar todas las
  capas anteriores pero **nunca al revés**.

---

## 6. Tests

- Usa **xUnit** + **FluentAssertions** + **NSubstitute**.
- Los tests E2E del Host usan `WebApplicationFactory<Program>` con
  `McpTestFactory` (sin base de datos real, sin llamadas reales al upstream).
- Cada test usa IDs únicos generados con `Guid.NewGuid()` para evitar colisiones
  de estado entre tests.
- Cubre siempre los caminos de error además del camino feliz.

---

*Este fichero se irá completando con nuevas reglas según evolucione el proyecto.*
