using TinyView.ViewModels;

namespace TinyView.Tests
{
    [TestFixture]
    public class ZoomStateTests
    {
        [Test]
        public void DefaultFactor_IsOne()
        {
            var zoom = new ZoomState();
            Assert.That(zoom.Factor, Is.EqualTo(1.0));
        }

        [Test]
        public void ZoomIn_DoublesFactorByButtonStep()
        {
            var zoom = new ZoomState();
            zoom.ZoomIn();
            Assert.That(zoom.Factor, Is.EqualTo(ZoomState.ButtonStep));
        }

        [Test]
        public void ZoomOut_HalvesFactorByButtonStep()
        {
            var zoom = new ZoomState();
            zoom.ZoomOut();
            Assert.That(zoom.Factor, Is.EqualTo(1.0 / ZoomState.ButtonStep));
        }

        [Test]
        public void Reset_RestoresDefaultFactor()
        {
            var zoom = new ZoomState();
            zoom.ZoomIn();
            zoom.ZoomIn();
            zoom.Reset();
            Assert.That(zoom.Factor, Is.EqualTo(ZoomState.DefaultFactor));
        }

        [Test]
        public void Factor_ClampedToMaxFactor()
        {
            var zoom = new ZoomState();
            zoom.Factor = ZoomState.MaxFactor * 10;
            Assert.That(zoom.Factor, Is.EqualTo(ZoomState.MaxFactor));
        }

        [Test]
        public void Factor_ClampedToMinFactor()
        {
            var zoom = new ZoomState();
            zoom.Factor = ZoomState.MinFactor / 10;
            Assert.That(zoom.Factor, Is.EqualTo(ZoomState.MinFactor));
        }

        [Test]
        public void CanZoomIn_FalseAtMaxFactor()
        {
            var zoom = new ZoomState();
            zoom.Factor = ZoomState.MaxFactor;
            Assert.That(zoom.CanZoomIn, Is.False);
        }

        [Test]
        public void CanZoomOut_FalseAtMinFactor()
        {
            var zoom = new ZoomState();
            zoom.Factor = ZoomState.MinFactor;
            Assert.That(zoom.CanZoomOut, Is.False);
        }

        [Test]
        public void CanZoomIn_TrueAtDefault()
        {
            var zoom = new ZoomState();
            Assert.That(zoom.CanZoomIn, Is.True);
        }

        [Test]
        public void CanZoomOut_TrueAtDefault()
        {
            var zoom = new ZoomState();
            Assert.That(zoom.CanZoomOut, Is.True);
        }

        [Test]
        public void Factor_RaisesPropertyChanged()
        {
            var zoom = new ZoomState();
            var changed = new List<string>();
            zoom.PropertyChanged += (_, e) => changed.Add(e.PropertyName ?? string.Empty);

            zoom.Factor = 4.0;

            Assert.That(changed, Does.Contain("Factor"));
            Assert.That(changed, Does.Contain("CanZoomIn"));
            Assert.That(changed, Does.Contain("CanZoomOut"));
        }
    }
}
