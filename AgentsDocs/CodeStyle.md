# Code Style — AnimalMerge

Reference: [Microsoft C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)

---

## Naming

| Element | Convention | Example |
|---|---|---|
| Class, struct, enum | PascalCase | `BattleStateMachine` |
| Interface | `I` + PascalCase | `IBattleState`, `IAnimalFactory` |
| Public method / property | PascalCase | `ExecuteTurnAsync()`, `IsAlive` |
| Private field | `_` + camelCase | `_battleService`, `_cancellationToken` |
| Local variable | camelCase | `targetCell`, `damage` |
| Parameter | camelCase | `cancellationToken`, `animalData` |
| Constant | PascalCase | `MaxHealth`, `GameGridId` |
| Event | PascalCase | `OnHealthChanged`, `OnUnitDied` |

---

## File and Class Structure

- One class per file; filename must match class name
- Explicit access modifiers always (`public`, `private`, `protected`, `internal`)
- Namespace mirrors folder path: `Code.Battle.Services`, `Framework.Code.Infrastructure`

---

## Local Variables — Use `var`

Use `var` when the type is obvious from the right-hand side:

```csharp
// Correct
var path = FindPath(start, end);
var installer = new BattleInstaller();
var cells = new List<GridCell>();

// Avoid — type is already explicit, no benefit
List<GridCell> path = FindPath(start, end);
```

Do **not** use `var` when the type is not obvious:
```csharp
// Avoid — reader cannot tell the type at a glance
var result = _service.Get();
```

Field declarations, method parameters, and return types always use explicit types.

---

## Ordering Within a Class

Recommended order:
1. Constants and static fields
2. Serialized fields (`[SerializeField]`)
3. Private fields
4. Properties
5. Unity lifecycle methods (`Awake`, `Start`, `OnEnable`, `OnDisable`, `Update`)
6. Public methods
7. Private methods
8. Nested types (if any)

---

## Async Methods

- Suffix async methods with `Async`: `ExecuteTurnAsync()`, `MoveAsync()`
- Always accept `CancellationToken` as the last parameter
- Return `UniTask` or `UniTask<T>`, not `Task`

---

## Comments

Write comments only when the **why** is non-obvious. Do not comment what the code does.

```csharp
// Correct — explains a non-obvious constraint
// HashSet deduplication prevents double-damage when OverlapCapsule returns the same collider twice
_hitTargets.Add(target);

// Avoid — states the obvious
// Subtract damage from health
_currentHealth -= damage;
```

---

## NaughtyAttributes Usage

Use `[NaughtyAttributes]` for inspector quality of life:
- `[Required]` — mark required references
- `[ShowIf]` / `[HideIf]` — conditional visibility
- `[Button]` — inspector test buttons
- `[Foldout]` — group serialized fields
