using TinyView.ViewModels;

namespace TinyView.Tests
{
    [TestFixture]
    public class AboutViewModelTests
    {
        [Test]
        public void AppIcon_IsNotNullOrEmpty()
        {
            var vm = new AboutViewModel();
            Assert.That(vm.AppIcon, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void AppName_IsNotNullOrEmpty()
        {
            var vm = new AboutViewModel();
            Assert.That(vm.AppName, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void Version_IsNotNullOrEmpty()
        {
            var vm = new AboutViewModel();
            Assert.That(vm.Version, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void RepoUrl_PointsToGitHub()
        {
            var vm = new AboutViewModel();
            Assert.That(vm.RepoUrl, Does.StartWith("https://github.com/"));
        }

        [Test]
        public void CloseCommand_CanExecute()
        {
            var vm = new AboutViewModel();
            Assert.That(vm.CloseCommand.CanExecute(null), Is.True);
        }

        [Test]
        public void CloseCommand_ExecutesWithoutError()
        {
            var vm = new AboutViewModel();
            Assert.DoesNotThrow(() => vm.CloseCommand.Execute(null));
        }
    }
}
