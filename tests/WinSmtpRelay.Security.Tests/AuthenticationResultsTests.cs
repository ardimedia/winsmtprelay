using WinSmtpRelay.Security.Models;

namespace WinSmtpRelay.Security.Tests;

[TestClass]
public class AuthenticationResultsTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public void ToHeaderValue_SpfPass_ContainsSpfPass()
    {
        var results = new AuthenticationResults(
            new SpfCheckResult(SpfVerdict.Pass, "ip4 matched"),
            new DmarcCheckResult(DmarcVerdict.None, DmarcPolicy.None, ""));

        var header = results.ToHeaderValue("winsmtprelay");
        Assert.IsTrue(header.Contains("spf=pass"));
        Assert.IsTrue(header.Contains("winsmtprelay"));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void ToHeaderValue_SpfAndDmarc_ContainsBoth()
    {
        var results = new AuthenticationResults(
            new SpfCheckResult(SpfVerdict.Pass, "matched"),
            new DmarcCheckResult(DmarcVerdict.Pass, DmarcPolicy.Reject, "aligned"));

        var header = results.ToHeaderValue("winsmtprelay");
        Assert.IsTrue(header.Contains("spf=pass"));
        Assert.IsTrue(header.Contains("dmarc=pass"));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void ShouldReject_SpfFail_ReturnsTrue()
    {
        var results = new AuthenticationResults(
            new SpfCheckResult(SpfVerdict.Fail, ""),
            new DmarcCheckResult(DmarcVerdict.None, DmarcPolicy.None, ""));

        Assert.IsTrue(results.ShouldReject);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void ShouldReject_SpfPass_ReturnsFalse()
    {
        var results = new AuthenticationResults(
            new SpfCheckResult(SpfVerdict.Pass, ""),
            new DmarcCheckResult(DmarcVerdict.None, DmarcPolicy.None, ""));

        Assert.IsFalse(results.ShouldReject);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void ShouldQuarantine_SpfSoftFail_ReturnsTrue()
    {
        var results = new AuthenticationResults(
            new SpfCheckResult(SpfVerdict.SoftFail, ""),
            new DmarcCheckResult(DmarcVerdict.None, DmarcPolicy.None, ""));

        Assert.IsTrue(results.ShouldQuarantine);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void ToHeaderValue_BothNone_ReturnsJustServerId()
    {
        var results = new AuthenticationResults(
            new SpfCheckResult(SpfVerdict.None, ""),
            new DmarcCheckResult(DmarcVerdict.None, DmarcPolicy.None, ""));

        var header = results.ToHeaderValue("winsmtprelay");
        Assert.AreEqual("winsmtprelay", header);
    }
}
