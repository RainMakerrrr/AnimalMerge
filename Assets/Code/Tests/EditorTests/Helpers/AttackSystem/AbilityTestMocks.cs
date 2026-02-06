using System;
using System.Threading.Tasks;
using Code.Abilities;
using Code.Animals;
using Code.Animals.Health;
using NSubstitute;
using UnityEngine;

namespace Code.Tests.EditorTests.Helpers.AttackSystem
{
    public static class AbilityTestMocks
    {
        /// <summary>
        /// Creates a real Dodge ability with mocked dependencies
        /// </summary>
        public static Dodge CreateDodge(bool isOwner, int initialCounter = 0)
        {
            var transformable = Substitute.For<ITransformable>();
            transformable.Shift().Returns(Task.CompletedTask);

            var go = new GameObject("DodgeTest");
            var collider = go.AddComponent<BoxCollider>();
            var colliders = new Collider[] { collider };

            var dodge = new Dodge(transformable, colliders, isOwner);

            // Set counter via reflection if needed
            if (initialCounter > 0)
            {
                var counterField = typeof(Dodge).GetField("_counter",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                counterField?.SetValue(dodge, initialCounter);
            }

            return dodge;
        }

        /// <summary>
        /// Creates a Dodge ability with controlled random behavior
        /// </summary>
        public static MockDodge CreateMockDodgeWithRNG(
            bool isOwner,
            int initialCounter = 0,
            Func<int> randomValueProvider = null)
        {
            var go = new GameObject("MockDodgeTest");
            var transformable = Substitute.For<ITransformable>();
            transformable.Shift().Returns(Task.CompletedTask);

            var collider = go.AddComponent<BoxCollider>();
            var colliders = new Collider[] { collider };

            var mockDodge = new MockDodge(transformable, colliders, isOwner, initialCounter, randomValueProvider);

            return mockDodge;
        }

        /// <summary>
        /// Creates a real CounterAttack ability with mocked dependencies
        /// </summary>
        public static CounterAttack CreateCounterAttack(
            AnimalHealth health,
            AnimalAnimator animator,
            AnimalAttack attack,
            bool isOwner)
        {
            return new CounterAttack(health, animator, attack, isOwner);
        }

        /// <summary>
        /// Creates a CounterAttack with mock dependencies for easier testing
        /// </summary>
        public static CounterAttack CreateMockCounterAttack(bool isOwner)
        {
            var healthGO = new GameObject("MockHealth");
            var health = healthGO.AddComponent<AnimalHealth>();

            var animatorGO = new GameObject("MockAnimator");
            var animator = animatorGO.AddComponent<AnimalAnimator>();

            var attackGO = new GameObject("MockAttack");
            var attack = attackGO.AddComponent<AnimalAttack>();

            return new CounterAttack(health, animator, attack, isOwner);
        }

        /// <summary>
        /// Creates a CounterAttack with controlled random behavior
        /// </summary>
        public static MockCounterAttack CreateMockCounterAttackWithRNG(
            AnimalHealth health,
            AnimalAnimator animator,
            AnimalAttack attack,
            bool isOwner,
            Func<int> randomValueProvider = null)
        {
            return new MockCounterAttack(health, animator, attack, isOwner, randomValueProvider);
        }
    }

    /// <summary>
    /// Mock Dodge with controllable RNG for testing probability
    /// </summary>
    public class MockDodge : IAbility
    {
        private readonly ITransformable _transformable;
        private readonly Collider[] _colliders;
        private readonly bool _isOwner;
        private int _counter;
        private readonly Func<int> _randomValueProvider;

        public bool IsBlockingDamage => true;
        public int Priority => 1;

        public int Counter => _counter;
        public int ApplyCallCount { get; private set; }

        public bool CanUse(AnimalAttack attacker)
        {
            // MockDodge does NOT work against AoE attacks
            if (attacker != null && attacker.IsAoE)
            {
                return false;
            }

            if (_counter == 0) return true;

            int successThreshold = _isOwner ? 80 : 50;
            int randomValue = _randomValueProvider?.Invoke() ?? UnityEngine.Random.Range(0, 100);
            return randomValue < successThreshold;
        }

        public MockDodge(
            ITransformable transformable,
            Collider[] colliders,
            bool isOwner,
            int initialCounter = 0,
            Func<int> randomValueProvider = null)
        {
            _transformable = transformable;
            _colliders = colliders;
            _isOwner = isOwner;
            _counter = initialCounter;
            _randomValueProvider = randomValueProvider;
        }

        public async Task Apply()
        {
            ApplyCallCount++;

            foreach (var collider in _colliders)
            {
                if (collider != null)
                    collider.enabled = false;
            }

            _counter++;
            await _transformable.Shift();
        }
    }

    /// <summary>
    /// Mock CounterAttack with controllable RNG for testing probability
    /// </summary>
    public class MockCounterAttack : IAbility
    {
        private readonly AnimalHealth _health;
        private readonly AnimalAnimator _animator;
        private readonly AnimalAttack _attack;
        private readonly bool _isOwner;
        private readonly Func<int> _randomValueProvider;

        public bool IsBlockingDamage => false;
        public int Priority => 0;

        public int ApplyCallCount { get; private set; }

        public bool CanUse(AnimalAttack attacker)
        {
            // MockCounterAttack does NOT work against AoE attacks
            if (attacker != null && attacker.IsAoE)
            {
                return false;
            }

            if (_isOwner) return true;

            int randomValue = _randomValueProvider?.Invoke() ?? UnityEngine.Random.Range(0, 100);
            return randomValue < 50;
        }

        public MockCounterAttack(
            AnimalHealth health,
            AnimalAnimator animator,
            AnimalAttack attack,
            bool isOwner,
            Func<int> randomValueProvider = null)
        {
            _health = health;
            _animator = animator;
            _attack = attack;
            _isOwner = isOwner;
            _randomValueProvider = randomValueProvider;
        }

        public async Task Apply()
        {
            ApplyCallCount++;

            if (_animator != null)
            {
                _animator.CounterAttackAnimation();
            }

            if (_health != null && _health.LastAttack != null)
            {
                var attackerHealth = _health.LastAttack.GetComponent<IDamageable>();
                if (attackerHealth != null)
                {
                    await attackerHealth.TakeDamageAsync(_attack);
                }
            }

            await Task.CompletedTask;
        }
    }
}
