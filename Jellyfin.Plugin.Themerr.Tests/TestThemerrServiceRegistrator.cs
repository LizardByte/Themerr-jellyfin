using MediaBrowser.Controller;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;

namespace Jellyfin.Plugin.Themerr.Tests;

public class TestThemerrServiceRegistrator
{
    [Fact]
    [Trait("Category", "Unit")]
    public void TestRegisterServices()
    {
        var registrator = new ThemerrServiceRegistrator();
        var services = new ServiceCollection();
        var mockApplicationHost = new Mock<IServerApplicationHost>();

        registrator.RegisterServices(services, mockApplicationHost.Object);

        Assert.Contains(services, sd => sd.ServiceType == typeof(IHostedService));
    }
}
