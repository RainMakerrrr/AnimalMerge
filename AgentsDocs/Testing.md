# Testing — AnimalMerge

## Stack
- **NUnit** — test framework
- **NSubstitute** — mocking
- **FluentAssertions** — assertions

---

## Test Location

| Type | Path |
|---|---|
| Editor (unit / integration) | `Assets/Code/Tests/EditorTests/` |
| Play Mode (runtime) | `Assets/Code/Tests/PlayModeTests/` |

Subfolders mirror the system under test: `GridPathfinding/`, `MergeSystem/`, `AttackAndDamageSystem/`, `BattleSystem/`, `Movement/`.

Shared helpers live in `EditorTests/Helpers/`: `GridTestHelper`, `ScenarioBuilder`, `TestFixtures`, `AssertionExtensions`.

---

## Test Structure

Follow Arrange / Act / Assert with clear comments:

```csharp
[TestFixture]
public class MergeUndoServiceTests
{
    private MergeUndoService _service;
    private IMergeCommand _mockCommand;

    [SetUp]
    public void SetUp()
    {
        _service = new MergeUndoService();
        _mockCommand = Substitute.For<IMergeCommand>();
    }

    [Test]
    public void ExecuteMerge_WhenEnabled_AddsCommandToStack()
    {
        // Arrange
        _service.Enable();

        // Act
        _service.ExecuteMerge(_mockCommand);

        // Assert
        _service.StackCount.Should().Be(1);
    }
}
```

---

## Mocking with NSubstitute

```csharp
var mockService = Substitute.For<IBattleService>();
mockService.CanExecuteAction(Arg.Any<ITurnAction>()).Returns(true);
```

---

## FluentAssertions Style

```csharp
result.Should().BeTrue("service should start disabled");
list.Should().HaveCount(3);
action.Should().Throw<InvalidOperationException>();
```

---

## What to Test

- Business logic: merge rules, damage calculation, victory conditions
- Pathfinding: `GetPossibleMoves`, `GetAllTargetCells`, edge cases for multi-size units
- State transitions: battle state machine, merge undo stack
- **Do not test**: Unity MonoBehaviour lifecycle, rendering, animation timings

---

## Guidelines

- Prefer EditorTests — they run faster and don't need the Unity runtime
- Use PlayModeTests only when testing MonoBehaviour lifecycle or physics
- Do not remove failing tests — fix the behavior or mark with `[Ignore]` with a reason
- Add or update tests when changing observable behavior
