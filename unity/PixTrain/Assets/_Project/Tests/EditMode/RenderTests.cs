using System.Collections.Generic;
using NUnit.Framework;
using PixTrain.Core.Data;
using PixTrain.Render;

namespace PixTrain.Core.Tests
{
    public class RenderTests
    {
        [Test]
        public void Blend_HalfAlpha_MixesColors()
        {
            uint c = Rgba.Blend(Rgba.Pack(0, 0, 0), Rgba.Pack(255, 100, 50), 128);
            Assert.That(Rgba.R(c), Is.EqualTo(128).Within(1));
            Assert.That(Rgba.G(c), Is.EqualTo(50).Within(1));
            Assert.That(Rgba.B(c), Is.EqualTo(25).Within(1));
            Assert.That(Rgba.A(c), Is.EqualTo(255));
        }

        [Test]
        public void Erase_CutsHolesIntoNightMask()
        {
            var mask = new PixelImage(4, 1);
            mask.Clear(Rgba.Pack(10, 12, 40, 200));
            var light = new PixelImage(2, 1);
            light.Put(0, 0, Rgba.Pack(0, 0, 0, 255));
            light.Put(1, 0, Rgba.Pack(0, 0, 0, 128));
            mask.Erase(light, 1, 0);

            Assert.That(Rgba.A(mask.Get(0, 0)), Is.EqualTo(200));
            Assert.That(Rgba.A(mask.Get(1, 0)), Is.EqualTo(0));
            Assert.That(Rgba.A(mask.Get(2, 0)), Is.EqualTo(99).Within(1));
        }

        [Test]
        public void BlitRegion_ClipsAtEdges()
        {
            var dst = new PixelImage(4, 4);
            var src = new PixelImage(3, 3);
            src.Clear(Rgba.Pack(1, 2, 3));
            dst.Blit(src, -2, 3);
            Assert.That(dst.Get(0, 3), Is.EqualTo(Rgba.Pack(1, 2, 3)));
            Assert.That(dst.Get(1, 3), Is.EqualTo(0u));
        }

        [Test]
        public void SpriteFactory_ReadsPaletteRows()
        {
            var img = SpriteFactory.FromRows(new[] { "k.", ".R" });
            Assert.That(img.Width, Is.EqualTo(2));
            Assert.That(img.Get(0, 0), Is.EqualTo(Palette.Get('k')));
            Assert.That(Rgba.A(img.Get(1, 0)), Is.EqualTo(0));
            Assert.That(img.Get(1, 1), Is.EqualTo(Palette.Get('R')));
        }

        [Test]
        public void EveryRoute_RendersDayAndNight([Values(0, 1, 2, 3, 4, 5)] int route)
        {
            var t = new TestGame();
            var run = t.G.Runs.StartRun(route, false, 40 + route);
            var view = new RunView(run, t.G);
            var renderer = new WorldRenderer();
            var screen = new PixelImage(WorldRenderer.Width, 300);
            for (int i = 0; i < 400; i++)
            {
                run.Fuel = run.FuelMax;
                run.TimeOfDay = i < 200 ? 0.5f : 0f;
                t.G.Runs.Tick(run, 1f / 20f);
                view.HandleEvents();
                view.Update(1f / 20f);
                if (i % 50 == 0 || i == 399) renderer.Render(screen, view, 300 - 92);
            }
            var colors = new HashSet<uint>(screen.Pixels);
            Assert.That(colors.Count, Is.GreaterThan(20), "sahne tek renk olmamalı");
            Assert.That(view.Scenery.Props.Count, Is.GreaterThan(0));
        }

        [Test]
        public void UiArt_ProducesExpectedSizes()
        {
            Assert.That(UiArt.Logo().Width, Is.EqualTo(102));
            var sky = UiArt.CitySkyline(3, 2);
            Assert.That(sky.Width, Is.EqualTo(42));
            Assert.That(UiArt.CitySkyline(3, 2), Is.SameAs(sky), "silüet önbellekten gelir");
            var train = UiArt.TrainPreview("diesel", new[] { WagonType.Passenger });
            Assert.That(train.Height, Is.EqualTo(28 + 21 + 6));
        }

        [Test]
        public void PixelFont_FoldsTurkishLetters()
        {
            var a = new PixelImage(30, 7);
            var b = new PixelImage(30, 7);
            PixelFont.DrawCentered(a, "ŞIĞ", 15, 1, Palette.Get('Y'), false);
            PixelFont.DrawCentered(b, "SIG", 15, 1, Palette.Get('Y'), false);
            Assert.That(a.Pixels, Is.EqualTo(b.Pixels));
        }
    }
}
