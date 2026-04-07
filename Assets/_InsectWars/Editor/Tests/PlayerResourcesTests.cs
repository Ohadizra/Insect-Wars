using InsectWars.RTS;
using NUnit.Framework;
using UnityEngine;

namespace InsectWars.Editor.Tests
{
    public class PlayerResourcesTests
    {
        [Test]
        public void TrySpend_ReducesWhenSufficient()
        {
            var go = new GameObject("PlayerResourcesTests_PR");
            var pr = go.AddComponent<PlayerResources>();
            pr.AddCalories(100);
            Assert.IsTrue(pr.TrySpend(40));
            Assert.AreEqual(60, pr.Calories);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void TrySpend_FailsWhenInsufficient()
        {
            var go = new GameObject("PlayerResourcesTests_PR2");
            var pr = go.AddComponent<PlayerResources>();
            pr.AddCalories(10);
            Assert.IsFalse(pr.TrySpend(20));
            Assert.AreEqual(10, pr.Calories);
            Object.DestroyImmediate(go);
        }
    }
}
