# AI Assistant Guidelines

This file is loaded automatically by Claude Code. If you are using a different AI
assistant (GitHub Copilot, Cursor, Windsurf, GPT, etc.), provide this file as
context before requesting changes. All rules here are mandatory for any AI working
on this repository.

---

## 1. Commit authorship

- **Never** add AI co-authorship trailers to commit messages.
  Do not use `Co-Authored-By` or mention "Claude", "GPT", "Copilot" or any other
  assistant in the Git history.
- The author of every commit is the human developer configured in Git:
  `AlbertoPyt <albertopyt1407@gmail.com>`.
- Commit messages must be written as if authored by the developer,
  in technical third person, never on behalf of an AI.

---

## 2. `using` directives — always in `GlobalUsings.cs`

- **Never** add `using` statements at the top of individual `.cs` class files.
- All `using` directives belong in the `GlobalUsings.cs` file of the corresponding
  project. Create it if it does not exist.
- Each project has its own `GlobalUsings.cs` at the project root.
- Do not duplicate usings already added implicitly by the SDK
  (`ImplicitUsings=enable` in the `.csproj`).
- If refactoring a using out of a class leaves it as the sole consumer,
  add it to `GlobalUsings.cs` anyway — consistency over micro-savings.

**Example structure:**
```
src/
  MCP.AzureDevOps.Domain/
    GlobalUsings.cs          ← usings for the Domain project
  MCP.AzureDevOps.Application/
    GlobalUsings.cs          ← usings for the Application project
  ...
```

---

## 3. Language

- **Everything** — code, comments, XML doc (`///`), log messages, error messages,
  test method names, and commit messages — must be written in **English**.

---

## 4. General code style

- Use **primary constructors** when a type only performs dependency injection
  (no additional constructor logic).
- Mark classes as **`sealed`** unless inheritance is explicitly intended.
- Use **`record`** for value objects and immutable DTOs.
- Do **not** override `ToString()` on value objects that contain sensitive data
  (tokens, passwords, keys).
- Prefer **`IReadOnlyList<T>`** / **`IReadOnlyDictionary<K,V>`** in public
  signatures; use `List<T>` / `Dictionary<K,V>` only in private implementations.

---

## 5. Architecture

This project follows **Clean Architecture / Hexagonal**. Dependency rules are strict:

```
Domain  ←  Application  ←  Infrastructure
                        ←  Host
                        ←  Cli
                        ←  Sdk
```

- `Domain` has no external NuGet dependencies.
- `Application` depends only on `Domain` and abstractions (`IOptions<T>` is
  allowed; `DbContext` is not).
- `Infrastructure` implements the ports defined in `Application`.
- `Host`, `Cli` and `Sdk` are entry points; they may reference all inner layers
  but **never the other way around**.

---

## 6. Tests

- Use **xUnit** + **FluentAssertions** + **NSubstitute**.
- Host E2E tests use `WebApplicationFactory<Program>` with `McpTestFactory`
  (no real database, no real upstream calls).
- Each test uses unique IDs generated with `Guid.NewGuid()` to avoid state
  collisions between tests.
- Always cover error paths in addition to the happy path.

---

*This file will be extended with new rules as the project evolves.*
