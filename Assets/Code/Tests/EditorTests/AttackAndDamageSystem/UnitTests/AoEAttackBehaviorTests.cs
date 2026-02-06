using System.Collections.Generic;
using Code.Animals;
using Code.Animals.Health;
using Code.Tests.EditorTests.Helpers.AttackSystem;
using FluentAssertions;
using NUnit.Framework;
using UnityEngine;

namespace Code.Tests.EditorTests.AttackAndDamageSystem.UnitTests
{
    [TestFixture]
    public class AoEAttackBehaviorTests
    {
        /// <summary>
        /// UT-AOE-ATTACK-BEHAVIOR-001: IsAoE_DefaultValue_IsFalse
        /// Verifies that IsAoE defaults to false for new attacks
        /// </summary>
        [Test]
        public void IsAoE_DefaultValue_IsFalse()
        {
            // Arrange & Act
            var attackerGO = new GameObject("Attacker");
            var attack = attackerGO.AddComponent<AnimalAttack>();

            // Assert
            attack.IsAoE.Should().BeFalse("IsAoE should default to false for single-target attacks");

            // Cleanup
            Object.DestroyImmediate(attackerGO);
        }

        /// <summary>
        /// UT-AOE-ATTACK-BEHAVIOR-002: IsAoE_SetToTrue_ReturnsTrue
        /// Verifies that IsAoE can be set to true for AoE attacks
        /// </summary>
        [Test]
        public void IsAoE_SetToTrue_ReturnsTrue()
        {
            // Arrange
            var attackerGO = new GameObject("Attacker");
            var attack = attackerGO.AddComponent<AnimalAttack>();

            // Act
            attack.SetIsAoE(true);

            // Assert
            attack.IsAoE.Should().BeTrue("IsAoE should be true after being set");

            // Cleanup
            Object.DestroyImmediate(attackerGO);
        }

        /// <summary>
        /// UT-AOE-ATTACK-BEHAVIOR-003: IsAoE_Flag_CorrectlySet
        /// Verifies that IsAoE flag can be set and retrieved
        /// NOTE: Physics-based attack behavior is tested in Play Mode integration tests
        /// </summary>
        [Test]
        public void IsAoE_Flag_CorrectlySet()
        {
            // Arrange
            var attackerGO = new GameObject("Attacker");
            var attack = attackerGO.AddComponent<AnimalAttack>();

            // Act & Assert - default is false
            attack.IsAoE.Should().BeFalse("IsAoE should default to false");

            // Act & Assert - can be set to true
            attack.SetIsAoE(true);
            attack.IsAoE.Should().BeTrue("IsAoE should be true after setting");

            // Act & Assert - can be set back to false
            attack.SetIsAoE(false);
            attack.IsAoE.Should().BeFalse("IsAoE should be false after resetting");

            // Cleanup
            Object.DestroyImmediate(attackerGO);
        }

        #region Helper Methods

        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(obj, value);
        }

        #endregion
    }
}
