using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WinSmtpRelay.Core.Configuration;
using WinSmtpRelay.Core.Interfaces;
using WinSmtpRelay.Delivery;
using WinSmtpRelay.Security;

namespace WinSmtpRelay.Delivery.Tests;

[TestClass]
public class DomainRoutingTests
{
    private SmtpDeliveryService CreateService(DeliveryOptions options)
    {
        var mxResolver = new StubMxResolver();
        var dkimSigner = new DkimSigningService(
            Options.Create(new DkimOptions()),
            NullLogger<DkimSigningService>.Instance);

        return new SmtpDeliveryService(
            mxResolver,
            Options.Create(options),
            dkimSigner,
            NullLogger<SmtpDeliveryService>.Instance);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void FindDomainRoute_ExactMatch_ReturnsRoute()
    {
        var options = new DeliveryOptions
        {
            DomainRoutes =
            [
                new DomainRouteOptions { DomainPattern = "example.com", Host = "relay.sendgrid.net", Port = 587 }
            ]
        };

        var service = CreateService(options);
        var route = service.FindDomainRoute("example.com");

        Assert.IsNotNull(route);
        Assert.AreEqual("relay.sendgrid.net", route.Host);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void FindDomainRoute_ExactMatch_CaseInsensitive()
    {
        var options = new DeliveryOptions
        {
            DomainRoutes =
            [
                new DomainRouteOptions { DomainPattern = "Example.COM", Host = "relay.sendgrid.net", Port = 587 }
            ]
        };

        var service = CreateService(options);
        var route = service.FindDomainRoute("example.com");
        Assert.IsNotNull(route);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void FindDomainRoute_WildcardMatch_Subdomain()
    {
        var options = new DeliveryOptions
        {
            DomainRoutes =
            [
                new DomainRouteOptions { DomainPattern = "*.example.com", Host = "smtp.brevo.com", Port = 587 }
            ]
        };

        var service = CreateService(options);
        var route = service.FindDomainRoute("sub.example.com");
        Assert.IsNotNull(route);
        Assert.AreEqual("smtp.brevo.com", route.Host);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void FindDomainRoute_WildcardMatch_BaseDomain()
    {
        var options = new DeliveryOptions
        {
            DomainRoutes =
            [
                new DomainRouteOptions { DomainPattern = "*.example.com", Host = "smtp.brevo.com", Port = 587 }
            ]
        };

        var service = CreateService(options);
        // *.example.com should also match example.com itself
        var route = service.FindDomainRoute("example.com");
        Assert.IsNotNull(route);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void FindDomainRoute_NoMatch_ReturnsNull()
    {
        var options = new DeliveryOptions
        {
            DomainRoutes =
            [
                new DomainRouteOptions { DomainPattern = "example.com", Host = "relay.sendgrid.net", Port = 587 }
            ]
        };

        var service = CreateService(options);
        var route = service.FindDomainRoute("other.com");
        Assert.IsNull(route);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void FindDomainRoute_EmptyRoutes_ReturnsNull()
    {
        var options = new DeliveryOptions();
        var service = CreateService(options);
        var route = service.FindDomainRoute("example.com");
        Assert.IsNull(route);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void FindDomainRoute_FirstMatchWins()
    {
        var options = new DeliveryOptions
        {
            DomainRoutes =
            [
                new DomainRouteOptions { DomainPattern = "example.com", Host = "first.host", Port = 587 },
                new DomainRouteOptions { DomainPattern = "example.com", Host = "second.host", Port = 587 }
            ]
        };

        var service = CreateService(options);
        var route = service.FindDomainRoute("example.com");
        Assert.IsNotNull(route);
        Assert.AreEqual("first.host", route.Host);
    }

    private class StubMxResolver : IMxResolver
    {
        public Task<IReadOnlyList<string>> ResolveMxAsync(string domain, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<string>>(["mx.example.com"]);
    }
}
