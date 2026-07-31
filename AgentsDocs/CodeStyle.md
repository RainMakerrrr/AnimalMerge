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

**Do not write comments.** No `//`, no `/* */`, no `///` XML docs. Express intent through naming and
structure instead: rename the variable, extract the block into a well-named method, split the
condition into a named local.

```csharp
// Avoid — a comment compensating for an unclear name
// Deduplicate so OverlapCapsule doesn't apply damage twice
_hitTargets.Add(target);

// Correct — the name carries the meaning, no comment needed
_alreadyDamagedThisAttack.Add(target);
```

This applies to **new and modified code**. Do not strip comments from code you are not otherwise
touching — that produces noise in the diff.

---

## NaughtyAttributes Usage

Use `[NaughtyAttributes]` for inspector quality of life:
- `[Required]` — mark required references
- `[ShowIf]` / `[HideIf]` — conditional visibility
- `[Button]` — inspector test buttons
- `[Foldout]` — group serialized fields
