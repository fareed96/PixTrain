using NUnit.Framework;
using PixTrain.Core.Data;
using PixTrain.Core.Services;
using PixTrain.Core.Utils;

namespace PixTrain.Core.Tests
{
    public class UtilsTests
    {
        [Test]
        public void SeededRandom_MatchesPrototypeSequence()
        {
            // Değerler prototipteki JavaScript mulberry32 ile üretildi.
            var r = new SeededRandom(12345);
            Assert.That(r.NextFloat(), Is.EqualTo(0.9797283f).Within(1e-6f));
            Assert.That(r.NextFloat(), Is.EqualTo(0.3067523f).Within(1e-6f));
            Assert.That(r.NextFloat(), Is.EqualTo(0.4842054f).Within(1e-6f));

            var n = new SeededRandom(-987654);
            Assert.That(n.NextFloat(), Is.EqualTo(0.5599240f).Within(1e-6f));
            Assert.That(n.NextFloat(), Is.EqualTo(0.6789167f).Within(1e-6f));
        }

        [Test]
        public void Hash_MatchesPrototype()
        {
            Assert.That(MathUtil.Hash(3, 7), Is.EqualTo(0.0607240f).Within(1e-6f));
            Assert.That(MathUtil.Hash(-5, 120), Is.EqualTo(0.0316230f).Within(1e-6f));
            Assert.That(MathUtil.Hash(100000, -3), Is.EqualTo(0.8069177f).Within(1e-6f));
        }

        [Test]
        public void SeededRandom_StaysInUnitRange()
        {
            var r = new SeededRandom(7);
            for (int i = 0; i < 100000; i++)
            {
                float v = r.NextFloat();
                Assert.That(v >= 0f && v < 1f, "değer aralık dışında: " + v);
                int k = r.NextInt(3);
                Assert.That(k >= 0 && k < 3);
            }
        }

        [Test]
        public void WeightedTable_RespectsWeights()
        {
            var table = new WeightedTable<string>().Add("a", 1f).Add("b", 3f).Add("c", 0f);
            var r = new SeededRandom(99);
            int a = 0, b = 0, c = 0;
            for (int i = 0; i < 20000; i++)
            {
                var v = table.Pick(r);
                if (v == "a") a++; else if (v == "b") b++; else c++;
            }
            Assert.That(c, Is.EqualTo(0));
            Assert.That((float)b / a, Is.EqualTo(3f).Within(0.25f));
        }

        [Test]
        public void RoundHalfUp_MatchesJavaScriptRounding()
        {
            Assert.That(MathUtil.RoundHalfUp(2.5f), Is.EqualTo(3));
            Assert.That(MathUtil.RoundHalfUp(3.5f), Is.EqualTo(4));
            Assert.That(MathUtil.RoundHalfUp(3.49f), Is.EqualTo(3));
        }

        [Test]
        public void DayCycle_NoonIsDay_MidnightIsNight()
        {
            Assert.That(DayCycle.Darkness(0.5f), Is.EqualTo(0f));
            Assert.That(DayCycle.Darkness(0f), Is.EqualTo(1f));
            Assert.That(DayCycle.Phase(0.5f), Is.EqualTo(DayPhase.Day));
            Assert.That(DayCycle.Phase(0f), Is.EqualTo(DayPhase.Night));
            Assert.That(DayCycle.Phase(0.75f), Is.EqualTo(DayPhase.Dusk));
            Assert.That(DayCycle.Phase(0.25f), Is.EqualTo(DayPhase.Dawn));
            Assert.That(DayCycle.IsNight(0.95f), Is.True);
            Assert.That(DayCycle.IsNight(0.4f), Is.False);
        }
    }
}
