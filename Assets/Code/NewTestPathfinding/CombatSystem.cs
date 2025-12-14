using UnityEngine;

namespace Assets.Code.NewTestPathfinding
{
    /// <summary>
    /// Система боя - управляет атаками и damage
    /// В данной реализации логика уже встроена в Unit, 
    /// этот класс можно использать как менеджер для эффектов и событий
    /// </summary>
    public class CombatSystem : MonoBehaviour
    {
        public static CombatSystem Instance { get; private set; }

        [Header("Combat Settings")]
        [SerializeField] private bool showCombatLog = true;
        [SerializeField] private bool spawnDamageNumbers = false;

        [Header("Effects")]
        [SerializeField] private GameObject hitEffectPrefab;
        [SerializeField] private GameObject deathEffectPrefab;

        // События для подписки
        public delegate void CombatEventHandler(Unit attacker, Unit target, int damage);
        public static event CombatEventHandler OnAttackPerformed;
        public static event CombatEventHandler OnDamageTaken;
        public static event System.Action<Unit> OnUnitDied;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Выполнить атаку с эффектами
        /// </summary>
        public void PerformAttack(Unit attacker, Unit target)
        {
            if (attacker == null || target == null)
                return;

            if (!attacker.CanAttackTarget(target))
                return;

            // Выполняем атаку
            attacker.Attack(target);

            // Вызываем событие
            OnAttackPerformed?.Invoke(attacker, target, attacker.Damage);

            // Спавним эффект
            if (hitEffectPrefab != null)
            {
                SpawnHitEffect(target.transform.position);
            }

            // Логирование
            if (showCombatLog)
            {
                Debug.Log($"[Combat] {attacker.name} атакует {target.name} на {attacker.Damage} урона!");
            }
        }

        /// <summary>
        /// Применить урон с эффектами
        /// </summary>
        public void ApplyDamage(Unit target, int damage, Unit source = null)
        {
            if (target == null || target.IsDead)
                return;

            bool wasAlive = !target.IsDead;
            target.TakeDamage(damage);

            // Вызываем событие
            OnDamageTaken?.Invoke(source, target, damage);

            // Если юнит умер
            if (wasAlive && target.IsDead)
            {
                OnUnitDeath(target);
            }
        }

        /// <summary>
        /// Обработка смерти юнита
        /// </summary>
        private void OnUnitDeath(Unit unit)
        {
            if (showCombatLog)
            {
                Debug.Log($"[Combat] {unit.name} погиб!");
            }

            // Вызываем событие
            OnUnitDied?.Invoke(unit);

            // Спавним эффект смерти
            if (deathEffectPrefab != null)
            {
                SpawnDeathEffect(unit.transform.position);
            }

            // Убираем юнита из Target Finding System
            TargetFindingSystem.Instance?.UnregisterUnit(unit);
        }

        /// <summary>
        /// Проверка возможности атаки
        /// </summary>
        public bool CanAttack(Unit attacker, Unit target)
        {
            if (attacker == null || target == null)
                return false;

            return attacker.CanAttackTarget(target);
        }

        /// <summary>
        /// Проверка радиуса атаки
        /// </summary>
        public bool IsInAttackRange(Unit attacker, Unit target)
        {
            if (attacker == null || target == null)
                return false;

            return attacker.IsInAttackRange(target);
        }

        /// <summary>
        /// Получить расстояние для атаки между юнитами
        /// </summary>
        public int GetAttackDistance(Unit attacker, Unit target)
        {
            if (attacker == null || target == null)
                return int.MaxValue;

            var attackerCells = attacker.GetOccupiedCells();
            var targetCells = target.GetOccupiedCells();

            int minDistance = int.MaxValue;

            foreach (var aCell in attackerCells)
            {
                foreach (var tCell in targetCells)
                {
                    // Chebyshev distance
                    int distance = Mathf.Max(
                        Mathf.Abs(aCell.x - tCell.x),
                        Mathf.Abs(aCell.y - tCell.y)
                    );

                    if (distance < minDistance)
                        minDistance = distance;
                }
            }

            return minDistance;
        }

        /// <summary>
        /// Спавн эффекта попадания
        /// </summary>
        private void SpawnHitEffect(Vector3 position)
        {
            if (hitEffectPrefab == null)
                return;

            GameObject effect = Instantiate(hitEffectPrefab, position, Quaternion.identity);
            Destroy(effect, 2f);
        }

        /// <summary>
        /// Спавн эффекта смерти
        /// </summary>
        private void SpawnDeathEffect(Vector3 position)
        {
            if (deathEffectPrefab == null)
                return;

            GameObject effect = Instantiate(deathEffectPrefab, position, Quaternion.identity);
            Destroy(effect, 3f);
        }

        /// <summary>
        /// Получить статистику боя
        /// </summary>
        public string GetCombatStats(Unit unit)
        {
            if (unit == null)
                return "Invalid unit";

            return $"HP: {unit.CurrentHealth}/{unit.MaxHealth}\n" +
                   $"Damage: {unit.Damage}\n" +
                   $"Attack Range: {unit.AttackRange}\n" +
                   $"Team: {unit.Team}";
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        #region Debug Visualization

        private void OnDrawGizmos()
        {
            // Можно добавить визуализацию атак в реальном времени
        }

        #endregion
    }

    /// <summary>
    /// Дополнительные утилиты для боевой системы
    /// </summary>
    public static class CombatUtility
    {
        /// <summary>
        /// Проверить, враждебны ли команды
        /// </summary>
        public static bool AreEnemies(Team team1, Team team2)
        {
            if (team1 == Team.Neutral || team2 == Team.Neutral)
                return false;

            return team1 != team2;
        }

        /// <summary>
        /// Рассчитать урон с учетом защиты (можно расширить)
        /// </summary>
        public static int CalculateDamage(int baseDamage, int defense = 0)
        {
            int finalDamage = Mathf.Max(1, baseDamage - defense);
            return finalDamage;
        }

        /// <summary>
        /// Проверить критический удар (можно расширить)
        /// </summary>
        public static bool IsCriticalHit(float critChance = 0.1f)
        {
            return Random.value < critChance;
        }
    }
}

