#nullable enable
using NUnit.Framework;
using CometaChampions.Core.Rules;

namespace CometaChampions.Tests.Editor
{
    [TestFixture]
    public class HandLayoutCalculatorTests
    {
        [Test]
        public void SingleCard_PositionedAtCenter_ZeroRotation()
        {
            var unselected = HandLayoutCalculator.CalculateTransform(0, 1, false);
            Assert.That(unselected.X, Is.EqualTo(0f));
            Assert.That(unselected.Y, Is.EqualTo(0f));
            Assert.That(unselected.ZAngle, Is.EqualTo(0f));
            Assert.That(unselected.Scale, Is.EqualTo(1f));

            var selected = HandLayoutCalculator.CalculateTransform(0, 1, true);
            Assert.That(selected.X, Is.EqualTo(0f));
            Assert.That(selected.Y, Is.EqualTo(HandLayoutCalculator.DefaultSelectedElevation));
            Assert.That(selected.ZAngle, Is.EqualTo(0f));
            Assert.That(selected.Scale, Is.GreaterThan(1f));
        }

        [Test]
        public void SixCards_FormsSymmetricRoundedArchAndFan()
        {
            const int total = 6;
            var c0 = HandLayoutCalculator.CalculateTransform(0, total, false);
            var c1 = HandLayoutCalculator.CalculateTransform(1, total, false);
            var c2 = HandLayoutCalculator.CalculateTransform(2, total, false);
            var c3 = HandLayoutCalculator.CalculateTransform(3, total, false);
            var c4 = HandLayoutCalculator.CalculateTransform(4, total, false);
            var c5 = HandLayoutCalculator.CalculateTransform(5, total, false);

            // Horizontal spacing symmetry
            Assert.That(c0.X, Is.EqualTo(-c5.X));
            Assert.That(c1.X, Is.EqualTo(-c4.X));
            Assert.That(c2.X, Is.EqualTo(-c3.X));
            Assert.That(c0.X, Is.LessThan(c1.X));
            Assert.That(c1.X, Is.LessThan(c2.X));
            Assert.That(c2.X, Is.LessThan(c3.X));

            // Vertical parabolic arch: center cards are higher than outer cards
            Assert.That(c2.Y, Is.GreaterThan(c1.Y));
            Assert.That(c1.Y, Is.GreaterThan(c0.Y));
            Assert.That(c0.Y, Is.EqualTo(c5.Y).Within(0.001f));
            Assert.That(c1.Y, Is.EqualTo(c4.Y).Within(0.001f));
            Assert.That(c2.Y, Is.EqualTo(c3.Y).Within(0.001f));

            // Rotational fan symmetry: left cards tilt counter-clockwise (+Z), right cards tilt clockwise (-Z)
            Assert.That(c0.ZAngle, Is.GreaterThan(0f));
            Assert.That(c5.ZAngle, Is.LessThan(0f));
            Assert.That(c0.ZAngle, Is.EqualTo(-c5.ZAngle).Within(0.001f));
            Assert.That(c1.ZAngle, Is.EqualTo(-c4.ZAngle).Within(0.001f));
            Assert.That(c2.ZAngle, Is.EqualTo(-c3.ZAngle).Within(0.001f));
            Assert.That(c0.ZAngle, Is.GreaterThan(c1.ZAngle));
            Assert.That(c1.ZAngle, Is.GreaterThan(c2.ZAngle));
        }

        [Test]
        public void FiveCards_CenterCardIsVerticalAndPeakOfArch()
        {
            const int total = 5;
            var middle = HandLayoutCalculator.CalculateTransform(2, total, false);
            Assert.That(middle.X, Is.EqualTo(0f).Within(0.001f));
            Assert.That(middle.Y, Is.EqualTo(HandLayoutCalculator.DefaultArchHeight).Within(0.001f));
            Assert.That(middle.ZAngle, Is.EqualTo(0f).Within(0.001f));

            var left = HandLayoutCalculator.CalculateTransform(0, total, false);
            var right = HandLayoutCalculator.CalculateTransform(4, total, false);
            Assert.That(left.Y, Is.LessThan(middle.Y));
            Assert.That(right.Y, Is.LessThan(middle.Y));
            Assert.That(left.ZAngle, Is.GreaterThan(0f));
            Assert.That(right.ZAngle, Is.LessThan(0f));
        }

        [Test]
        public void SelectedCard_ElevatedAndStraightened()
        {
            const int total = 6;
            var unselected = HandLayoutCalculator.CalculateTransform(0, total, false);
            var selected = HandLayoutCalculator.CalculateTransform(0, total, true);

            Assert.That(selected.Y, Is.EqualTo(unselected.Y + HandLayoutCalculator.DefaultSelectedElevation));
            Assert.That(selected.Scale, Is.GreaterThan(unselected.Scale));
            Assert.That(selected.ZAngle, Is.LessThan(unselected.ZAngle)); // Straightened
        }

        [Test]
        public void OutOfBounds_IndexOrEmpty_ReturnsSafeDefault()
        {
            var empty = HandLayoutCalculator.CalculateTransform(0, 0, false);
            Assert.That(empty.X, Is.EqualTo(0f));
            Assert.That(empty.Y, Is.EqualTo(0f));
            Assert.That(empty.ZAngle, Is.EqualTo(0f));

            var negative = HandLayoutCalculator.CalculateTransform(-1, 5, false);
            Assert.That(negative.X, Is.EqualTo(0f));

            var overflow = HandLayoutCalculator.CalculateTransform(5, 5, false);
            Assert.That(overflow.X, Is.EqualTo(0f));
        }
    }
}
