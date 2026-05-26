# Unity Patterns — AnimalMerge

## Component Caching (Mandatory)

Cache all component references in `Awake`. Never call `GetComponent` in Update loops.

```csharp
private Animator _animator;
private SpriteRenderer _spriteRenderer;

private void Awake()
{
    _animator = GetComponent<Animator>();
    _spriteRenderer = GetComponent<SpriteRenderer>();
}
```

Never:
```csharp
void Update() { GetComponent<Animator>().Play("Attack"); } // forbidden
```

---

## UniTask + CancellationToken

All async operations use `UniTask`. Provide cancellation via `OnEnable`/`OnDisable`:

```csharp
private CancellationTokenSource _cts;

private void OnEnable()  => _cts = new CancellationTokenSource();
private void OnDisable() { _cts?.Cancel(); _cts?.Dispose(); }

private async UniTask DoSomethingAsync()
{
    await SomeOperationAsync(_cts.Token);
}
```

Always handle `OperationCanceledException` in top-level async entry points.

---

## Events Over Polling

Use C# events instead of comparing state in Update:

```csharp
// Correct
public event Action<float> OnHealthChanged;

public void TakeDamage(float damage)
{
    _currentHealth -= damage;
    OnHealthChanged?.Invoke(_currentHealth);
}

// Avoid
void Update() { if (_health != _prev) UpdateUI(); } // don't do this
```

---

## Object Pooling

Required for: VFX effects, projectiles, damage number UI, UI popups.
Framework provides `WindowPool` for UI windows. For game objects, implement via `Queue<T>` and Zenject's `DiContainer`.

Return objects to pool via `gameObject.SetActive(false)`.

---

## Physics

- Physics operations (`MovePosition`, `AddForce`) in `FixedUpdate`
- Always use `LayerMask` for raycasts and overlaps — never query all layers
- Damage detection uses `OverlapCapsule` / `OverlapSphere` with a `HashSet` to deduplicate hits within a single attack

---

## UI

- Use `TextMeshProUGUI`, not legacy `Text`
- Show/hide panels via `CanvasGroup` (alpha + interactable + blocksRaycasts), not `SetActive`
- Batch UI updates — set all fields in one method call, not one per frame

---

## Forbidden in Update / FixedUpdate / LateUpdate

```
FindObjectOfType<T>()
GameObject.Find()
GetComponent<T>()      — cache in Awake instead
Camera.main            — cache in Awake instead
Resources.Load()
Instantiate()          — use pooling instead
transform.Find()       — cache references
```

---

## Resources.Load()

Use only when a prefab must be loaded at runtime by path. Place asset in `Assets/Resources/<subfolder>/`.

```csharp
// AnimalDatabase is loaded this way via Zenject installer
Container.Bind<AnimalDatabase>()
    .FromScriptableObjectResource("AnimalDatabase")
    .AsSingle();
```

For everything else, use Zenject factories and direct prefab references.
