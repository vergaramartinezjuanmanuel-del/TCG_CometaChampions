using NUnit.Framework;
using CometaChampions.Core.Enums;
using CometaChampions.Core.Rules;

namespace CometaChampions.Tests
{
    [TestFixture]
    public class ClanAdvantageTests
    {
        [Test]
        [TestCase(ClanType.Milenarios, ClanType.Fugitivos, ExpectedResult = true)]
        [TestCase(ClanType.Fugitivos, ClanType.Depredadores, ExpectedResult = true)]
        [TestCase(ClanType.Depredadores, ClanType.Miticos, ExpectedResult = true)]
        [TestCase(ClanType.Miticos, ClanType.Milenarios, ExpectedResult = true)]
        [TestCase(ClanType.Fugitivos, ClanType.Milenarios, ExpectedResult = false)]
        [TestCase(ClanType.Depredadores, ClanType.Fugitivos, ExpectedResult = false)]
        [TestCase(ClanType.Miticos, ClanType.Depredadores, ExpectedResult = false)]
        [TestCase(ClanType.Milenarios, ClanType.Miticos, ExpectedResult = false)]
        [TestCase(ClanType.Milenarios, ClanType.Milenarios, ExpectedResult = false)]
        [TestCase(ClanType.Neutral, ClanType.Milenarios, ExpectedResult = false)]
        public bool CirculoCometa_AdvantageEvaluation(ClanType attacker, ClanType defender)
        {
            return ClanAdvantage.HasAdvantage(attacker, defender);
        }

        [Test]
        public void CalculateAttackDamage_AppliesBonus_WhenAdvantageIsPresent()
        {
            int baseDamage = 5;
            int totalDamage = ClanAdvantage.CalculateAttackDamage(baseDamage, ClanType.Depredadores, ClanType.Miticos);
            Assert.AreEqual(6, totalDamage, "Depredadores should deal +1 damage against Míticos.");
        }

        [Test]
        public void CalculateAttackDamage_DoesNotApplyBonus_WhenNoAdvantage()
        {
            int baseDamage = 5;
            int totalDamage = ClanAdvantage.CalculateAttackDamage(baseDamage, ClanType.Depredadores, ClanType.Fugitivos);
            Assert.AreEqual(5, totalDamage, "Depredadores should deal normal damage against Fugitivos.");
        }
    }
}
