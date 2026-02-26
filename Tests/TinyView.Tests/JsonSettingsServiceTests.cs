using TinyView.Models;
using TinyView.Services;

namespace TinyView.Tests
{
    [TestFixture]
    public class JsonSettingsServiceTests
    {
        private string _tempDir = null!;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "TinyViewTests_" + Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }

        [Test]
        public void SaveAndLoad_RoundTripsAllProperties()
        {
            var service = new JsonSettingsService(_tempDir);

            var original = new UserSettings
            {
                Width = 1024,
                Height = 768,
                Left = 100,
                Top = 200,
                IsMaximized = true,
                SelectedPaletteName = "Viridis"
            };

            service.Save(original);
            var loaded = service.Load();

            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded!.Width, Is.EqualTo(original.Width));
            Assert.That(loaded.Height, Is.EqualTo(original.Height));
            Assert.That(loaded.Left, Is.EqualTo(original.Left));
            Assert.That(loaded.Top, Is.EqualTo(original.Top));
            Assert.That(loaded.IsMaximized, Is.EqualTo(original.IsMaximized));
            Assert.That(loaded.SelectedPaletteName, Is.EqualTo(original.SelectedPaletteName));
        }

        [Test]
        public void SaveAndLoad_RoundTripsDefaultValues()
        {
            var service = new JsonSettingsService(_tempDir);

            var original = new UserSettings();

            service.Save(original);
            var loaded = service.Load();

            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded!.Width, Is.EqualTo(800));
            Assert.That(loaded.Height, Is.EqualTo(600));
            Assert.That(loaded.IsMaximized, Is.False);
            Assert.That(loaded.SelectedPaletteName, Is.EqualTo(string.Empty));
        }

        [Test]
        public void SaveAndLoad_RoundTripsNaNPositionValues()
        {
            var service = new JsonSettingsService(_tempDir);

            var original = new UserSettings
            {
                Left = double.NaN,
                Top = double.NaN
            };

            service.Save(original);
            var loaded = service.Load();

            Assert.That(loaded, Is.Not.Null);
            Assert.That(double.IsNaN(loaded!.Left), Is.True);
            Assert.That(double.IsNaN(loaded.Top), Is.True);
        }

        [Test]
        public void Load_ReturnsNull_WhenNoFileExists()
        {
            var service = new JsonSettingsService(_tempDir);

            var result = service.Load();

            Assert.That(result, Is.Null);
        }

        [Test]
        public void Save_OverwritesPreviousSettings()
        {
            var service = new JsonSettingsService(_tempDir);

            service.Save(new UserSettings { Width = 640, SelectedPaletteName = "Gray" });
            service.Save(new UserSettings { Width = 1920, SelectedPaletteName = "Turbo" });

            var loaded = service.Load();

            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded!.Width, Is.EqualTo(1920));
            Assert.That(loaded.SelectedPaletteName, Is.EqualTo("Turbo"));
        }

        [Test]
        public void Load_ReturnsNull_WhenFileContainsInvalidJson()
        {
            Directory.CreateDirectory(_tempDir);
            File.WriteAllText(Path.Combine(_tempDir, "UserSettings.json"), "not valid json{{{");

            var service = new JsonSettingsService(_tempDir);
            var result = service.Load();

            Assert.That(result, Is.Null);
        }

        [Test]
        public void Save_CreatesDirectoryIfMissing()
        {
            var nested = Path.Combine(_tempDir, "sub", "folder");
            var service = new JsonSettingsService(nested);

            service.Save(new UserSettings { Width = 500 });

            Assert.That(Directory.Exists(nested), Is.True);
            Assert.That(service.Load()?.Width, Is.EqualTo(500));
        }
    }
}
