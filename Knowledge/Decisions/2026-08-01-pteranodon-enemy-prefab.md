# Решение: враг Pteranodon собран как обычный префаб, футпринт оставлен 1х1

**Дата:** 2026-08-01
**Статус:** принято, реализовано (префаб готов; в бой не попадает — не подключён ни к одному `LevelStageConfig`)

## Контекст

На сцене лежал «сырой» `GameObject` птеранодона — меш из ассет-пака polyperfect без единого
геймплейного компонента. Нужен был полноценный вражеский префаб по образцу
`Assets/Resources/Prefabs/Enemies/T-Rex.prefab` / `Velociraptor.prefab`.

Побочно всплыли два расхождения с тем, что было записано в [[Index]]:

- Спека 06 §2.5 требует для птеродактиля футпринт **2х1**, а прошлая сессия записала «префаба
  птеродактиля в проекте нет вообще, требование неприменимо». Префаб появился — требование стало применимо.
- `PterodactylStats.asset` на диске содержит **200 HP / 200 dmg / 3 клетки**, тогда как заметка
  [[2026-08-01-chicken-flock-and-balance-sync]] утверждала, что применены 350 HP / 5 клеток.
  **[Закрыто 2026-08-02]** Правка действительно не сохранилась — и не она одна: до диска не доехала
  ни одна балансная правка §2.1–2.5. Ассет приведён к доку (**350 / 200 / 5**) и проверен на диске.

## Рассмотренные варианты

**1. Форма префаба.**
- *Variant от FBX-меша* — как `EnemyCheetah_Temp`. Отвергнуто: `T-Rex` и `Velociraptor` — обычные
  префабы, а паритет с ними важнее, чем наследование от импортированной модели.
- ✅ *Обычный (не-вариантный) префаб* из объекта сцены.

**2. Футпринт `_unitSize`.**
- *2х1 по спеке 06 §2.5* — реализовано первым заходом, **откачено после ревью**. Причины ниже.
- ✅ *1х1.* Единственный размер, который стек сетки обрабатывает корректно для этого юнита.

**3. Куда класть новые клипы и переписанный контроллер.**
- *Скопировать контроллер в `Assets/Code`/`Assets/Settings`* — пережило бы ре-импорт пакета,
  но разошлось бы с существующей практикой.
- ✅ *Править `Pterodactyl.controller` на месте в папке вендора и класть клипы рядом с мешем* —
  ровно так уже сделаны `Trex.controller` и `Velociraptor.controller`. Цена принята: ре-импорт
  polyperfect-пакета сотрёт правки.

## Принятое решение

### Префаб

`Assets/Resources/Prefabs/Enemies/Pteranodon.prefab` — канонический набор компонентов врага:
`Animator`, `PterodactylFacade`, `AnimalMovement`, `UnitOccupancy`, `AnimalAttack`, `AnimalHealth`,
`AnimalAnimator`, `DamagePopupController`; дети `Collider` (слой 23, не-триггерный `SphereCollider`),
`AttackPoint` и вложенный инстанс префаба `HealthBar`. Корень: слой 23 (`Enemy`), тег `Untagged`,
scale 0.8. Объект со сцены удалён, `Main Scene` **не коммитилась**.

Новый `Assets/Code/Animals/Facades/PterodactylFacade.cs` — `: EnemyAnimalFacade` с пустым
`InitBehaviours()`, по образцу `TRexFacade`. Нового Zenject-биндинга не потребовалось: наследуется
`AnimalFacade.Construct(IRandomProvider)`.

`HealthBarSetupTool.HeightOffsets` получил `{ "Pteranodon", 1.3f }`.

### Анимации

- `Pteranodon_Attack.anim` — копия `Pterodactyl_Jump.anim` (2.5 с, loop off) с обязательным
  анимационным событием `AttackAnimationHandler` на `t = 1.5`.
- `Pteranodon_Death.anim` — синтезирована: все кривые заморожены на `t = 0` плюс поворот `Group/Main`
  на 90° по Z за 0.9 с, общая длина 1.2 с. **Нужен арт-пасс.**
- `Pterodactyl.controller` переписан: 7 параметров как в `Trex.controller` (`MoveSpeed` Float,
  `Attack`/`TakeDamage`/`Jump`/`CounterAttack` Trigger, `IsDead` Bool, `TurnDirection` Float),
  Idle ⇄ Flap по `MoveSpeed ≷ 0.1`, → Attack по триггеру и **Any State → Death по `IsDead == true`**.
  Удалены wander-булевы пакета и 5 осиротевших сабассетов `AnimatorStateTransition`.

> [!danger] `UnitSize {2,1}` НЕ поддерживается стеком сетки — это живая ловушка
> `TargetPositionCalculator.GetAnchorPointsForCell` (`TargetPositionCalculator.cs:255`) ветвится
> только на `UnitSize.Small (1,1)`, `Medium (1,2)` и `Large (2,2)` (`UnitSize.cs:22-24`).
> `{2,1}` не совпадает ни с одним → пустой список якорей → `GetPossibleAttackPositions` пуст →
> `AnimalMovement.Move` возвращает `false` → `TurnExecutor` превращает весь ход врага в no-op:
> юнит никогда не ходит и никогда не атакует.
> Дополнительно: `GridManager.GetOccupiedCellsInternal` (`GridManager.cs:468-495`) для
> прямоугольных размеров использует только `size.Height`, поэтому `{2,1}` занимает **одну** клетку,
> тогда как `GridManager.AdjustPositionToFitBounds` (`:392-403`) `Width = 2` учитывает — код
> внутренне несогласован. И `Utilities.GetMovementOffset` (`Utilities.cs:117-120`) имеет ветку 2х1,
> возвращающую `(0.5, 0, 0)` для `Direction.South`, — меш отрисуется на полклетки в стороне.
> **Безопасны только 1х1, 1х2 и 2х2.** Спека 06 §2.5 заблокирована до расширения
> `UnitSize` / `GridManager` / `TargetPositionCalculator`.
> Иронично, что `TargetPositionCalculatorTests` — ровно та сьюта, которая поймала бы это,
> и она красная предсуществующе.

> [!danger] Анимационное событие `AttackAnimationHandler` обязательно на КАЖДОМ новом клипе атаки
> `AnimalAttack.Attack(ITarget)` (`AnimalAttack.cs:157`) крутит `while (!_isAttackDone) await UniTask.Yield();`,
> а `_isAttackDone` выставляется только внутри `AttackAnimationHandlerAsync`, куда попадают
> исключительно по этому событию. Без него ход врага виснет навсегда — это в точности
> известный баг оленя. Ставить на ~55–65 % клипа и следить, чтобы exit time исходящего перехода
> был позже события.

> [!warning] `EnemySpawnService.cs:107` ставит только слой КОРНЯ
> Дети сохраняют сериализованные слои, поэтому дочерний `Collider` обязан быть авторингом на слое 23,
> иначе атаки игрока (маска `1 << 23` = `8388608`) в него никогда не попадут.
> `AnimalAttack._mask` у врага должен быть `1048576` (`1 << 20`, Animal).

## Почему так

- **Паритет важнее буквы спеки.** Набор компонентов и место хранения выведены из уже работающих
  `T-Rex`/`Velociraptor`, а не придуманы заново, — так новый враг ведёт себя предсказуемо.
- **Футпринт 2х1 не «не успели», а физически ломает юнита.** Реализовать его как поле — одна строка;
  но результат — молча неиграбельный враг. Пока три места в стеке сетки трактуют `{2,1}` по-разному,
  правильный ход — оставить 1х1 и записать блокер.
- **Клипы и контроллер в папке вендора** — не потому, что это хорошо, а потому, что альтернатива
  создала бы третью, четвёртую практику размещения при уже существующих двух прецедентах.

## Проверка

- Unity Console — ноль ошибок компиляции.
- Действует **TESTS PAUSED** — новых тестов не писали.
- Предсуществующие красные EditMode-тесты (**не** регрессии, 25 шт.): `TargetPositionCalculatorTests`,
  `RetreatAbilityTests`, `AnimalAttackPostAbilityTests` — две последние падают с
  «Method has non-void return value, but no result is expected» (проблема сигнатуры async-теста).
- **Не проверено в Play Mode:** префаб не подключён ни к одному `LevelStageConfig`, поэтому
  в бою пока не спавнится вообще.

> [!note] Предсуществующие баги, найденные и намеренно НЕ починенные
> - `Velociraptor.controller` объявляет `IsDead` как **Trigger** (`m_Type: 9`), тогда как
>   `AnimalAnimator.DeathAnimation()` зовёт `SetBool(IsDead, true)`.
> - `Trex.controller` и `Velociraptor.controller` оба имеют `m_AnyStateTransitions: []` —
>   разводка Any State → Death есть только у нового контроллера птеранодона.

## Затронутые файлы

**Новые:**
`Assets/Resources/Prefabs/Enemies/Pteranodon.prefab` (+ `.meta`);
`Assets/Code/Animals/Facades/PterodactylFacade.cs` (+ `.meta`);
`Assets/polyperfect/Low Poly Animated Dinos/- Mesh/Dinos/Pteranodon/Pteranodon_Attack.anim` (+ `.meta`);
`Assets/polyperfect/Low Poly Animated Dinos/- Mesh/Dinos/Pteranodon/Pteranodon_Death.anim` (+ `.meta`).

**Изменены:**
`Assets/polyperfect/Low Poly Animated Dinos/- Animator Controllers/Pterodactyl.controller`;
`Assets/Code/Editor/HealthBarSetupTool.cs`.

**Намеренно не тронуты:** `Assets/Settings/Animals/Stats/PterodactylStats.asset` (200 / 200 / 3 —
вне скоупа), `Assets/Scenes/Main Scene.unity` (лишний объект удалён, сцена не сохранялась).
