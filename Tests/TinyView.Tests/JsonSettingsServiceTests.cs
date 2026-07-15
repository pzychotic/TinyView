using TinyView.Models;
using TinyView.Services;

namespace TinyView.Tests;

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

        var original = new AppSettings
        {
            WindowLeft = 100,
            WindowTop = 200,
            WindowWidth = 1024,
            WindowHeight = 768,
            WindowMaximized = true,
            SelectedPaletteName = "Viridis"
        };

        service.Save(original);
        var loaded = service.Load();

        Assert.That(loaded, Is.Not.Null);
        Assert.That(loaded!.WindowLeft, Is.EqualTo(original.WindowLeft));
        Assert.That(loaded.WindowTop, Is.EqualTo(original.WindowTop));
        Assert.That(loaded.WindowWidth, Is.EqualTo(original.WindowWidth));
        Assert.That(loaded.WindowHeight, Is.EqualTo(original.WindowHeight));
        Assert.That(loaded.WindowMaximized, Is.EqualTo(original.WindowMaximized));
        Assert.That(loaded.SelectedPaletteName, Is.EqualTo(original.SelectedPaletteName));
    }

    [Test]
    public void SaveAndLoad_RoundTripsDefaultValues()
    {
        var service = new JsonSettingsService(_tempDir);

        var original = new AppSettings();

        service.Save(original);
        var loaded = service.Load();

        Assert.That(loaded, Is.Not.Null);
        Assert.That(loaded!.WindowLeft, Is.Null);
        Assert.That(loaded.WindowTop, Is.Null);
        Assert.That(loaded.WindowWidth, Is.Null);
        Assert.That(loaded.WindowHeight, Is.Null);
        Assert.That(loaded.WindowMaximized, Is.False);
        Assert.That(loaded.SelectedPaletteName, Is.Null);
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

        service.Save(new AppSettings { WindowWidth = 640, SelectedPaletteName = "Gray" });
        service.Save(new AppSettings { WindowWidth = 1920, SelectedPaletteName = "Turbo" });

        var loaded = service.Load();

        Assert.That(loaded, Is.Not.Null);
        Assert.That(loaded!.WindowWidth, Is.EqualTo(1920));
        Assert.That(loaded.SelectedPaletteName, Is.EqualTo("Turbo"));
    }

    [Test]
    public void Load_ReturnsNull_WhenFileContainsInvalidJson()
    {
        Directory.CreateDirectory(_tempDir);
        File.WriteAllText(Path.Combine(_tempDir, "Settings.json"), "not valid json{{{");

        var service = new JsonSettingsService(_tempDir);
        var result = service.Load();

        Assert.That(result, Is.Null);
    }

    [Test]
    public void Save_CreatesDirectoryIfMissing()
    {
        var nested = Path.Combine(_tempDir, "sub", "folder");
        var service = new JsonSettingsService(nested);

        service.Save(new AppSettings { WindowWidth = 500 });

        Assert.That(Directory.Exists(nested), Is.True);
        Assert.That(service.Load()?.WindowWidth, Is.EqualTo(500));
    }

    [Test]
    public void Load_WhenIOErrorOccurs_ReturnsNullGracefully()
    {
        Directory.CreateDirectory(_tempDir);
        var settingsPath = Path.Combine(_tempDir, "Settings.json");

        // Create an exclusive lock on the file so an IOException is thrown on read attempts
        using var fs = new FileStream(settingsPath, FileMode.Create, FileAccess.Write, FileShare.None);
        fs.Write([1, 2, 3]);

        var service = new JsonSettingsService(_tempDir);
        var result = service.Load();

        Assert.That(result, Is.Null);
    }

    [Test]
    public void Save_WhenIOErrorOccurs_HandlesExceptionGracefully()
    {
        Directory.CreateDirectory(_tempDir);
        var settingsPath = Path.Combine(_tempDir, "Settings.json");

        // Prevent writing to settings by creating a directory where the file should be
        Directory.CreateDirectory(settingsPath);

        var service = new JsonSettingsService(_tempDir);
        var original = new AppSettings { WindowWidth = 1024 };

        Assert.DoesNotThrow(() => service.Save(original));
    }
}
