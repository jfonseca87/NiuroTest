using Microsoft.Extensions.DependencyInjection;
using Moq;
using Niuro.Core.Domain.Queries;
using Niuro.Core.Domain.Rules;
using CoreRuleEngine = Niuro.Core.Domain.Rules.RuleEngine;

namespace Niuro.Tests.RuleEngine;

public class StateNyRuleTests
{
    [Theory]
    [InlineData("NY", true)]
    [InlineData("CA", false)]
    [InlineData("ny", false)] // Case sensitive: uppercase only
    public async Task Applies_ReturnsExpected(string state, bool expected)
    {
        var rule = new StateNyRule();
        var candidate = new LoanCandidate { Ssn = "123-45-6789", State = state };

        var result = await rule.AppliesAsync(candidate);

        Assert.Equal(expected, result);
    }
}

public class BlacklistedSsnRuleTests
{
    [Fact]
    public async Task Applies_WhenSsnIsBlacklisted_ReturnsTrue()
    {
        var mockQuery = new Mock<IBlacklistedSsnQuery>();
        mockQuery.Setup(q => q.IsBlacklistedAsync("123-45-6789", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var rule = new BlacklistedSsnRule(mockQuery.Object);
        var candidate = new LoanCandidate { Ssn = "123-45-6789", State = "CA" };

        var result = await rule.AppliesAsync(candidate);

        Assert.True(result);
    }

    [Fact]
    public async Task Applies_WhenSsnIsNotBlacklisted_ReturnsFalse()
    {
        var mockQuery = new Mock<IBlacklistedSsnQuery>();
        mockQuery.Setup(q => q.IsBlacklistedAsync("123-45-6789", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var rule = new BlacklistedSsnRule(mockQuery.Object);
        var candidate = new LoanCandidate { Ssn = "123-45-6789", State = "CA" };

        var result = await rule.AppliesAsync(candidate);

        Assert.False(result);
    }
}

public class RuleEngineTests
{
    [Fact]
    public async Task Evaluate_WhenNoRuleApplies_ReturnsApproved()
    {
        var mockQuery = new Mock<IBlacklistedSsnQuery>();
        mockQuery.Setup(q => q.IsBlacklistedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var rules = new IDenialRule[]
        {
            new StateNyRule(),
            new BlacklistedSsnRule(mockQuery.Object)
        };
        var engine = new CoreRuleEngine(rules);
        var candidate = new LoanCandidate { Ssn = "123-45-6789", State = "CA" };

        var result = await engine.EvaluateAsync(candidate);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Evaluate_WhenStateNy_ReturnsDeniedWithStateNyReason()
    {
        var mockQuery = new Mock<IBlacklistedSsnQuery>();
        mockQuery.Setup(q => q.IsBlacklistedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var rules = new IDenialRule[]
        {
            new StateNyRule(),
            new BlacklistedSsnRule(mockQuery.Object)
        };
        var engine = new CoreRuleEngine(rules);
        var candidate = new LoanCandidate { Ssn = "123-45-6789", State = "NY" };

        var result = await engine.EvaluateAsync(candidate);

        Assert.True(result.IsFailure);
        Assert.Equal("STATE_NY", result.Error);
    }

    [Fact]
    public async Task Evaluate_WhenSsnBlacklisted_ReturnsDeniedWithSsnBlacklistedReason()
    {
        var mockQuery = new Mock<IBlacklistedSsnQuery>();
        mockQuery.Setup(q => q.IsBlacklistedAsync("123-45-6789", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var rules = new IDenialRule[]
        {
            new StateNyRule(),
            new BlacklistedSsnRule(mockQuery.Object)
        };
        var engine = new CoreRuleEngine(rules);
        var candidate = new LoanCandidate { Ssn = "123-45-6789", State = "CA" };

        var result = await engine.EvaluateAsync(candidate);

        Assert.True(result.IsFailure);
        Assert.Equal("SSN_BLACKLISTED", result.Error);
    }

    [Fact]
    public async Task Evaluate_FirstMatchingRuleWins()
    {
        // If both rules apply, the first one wins (StateNY in this case, by order)
        var mockQuery = new Mock<IBlacklistedSsnQuery>();
        mockQuery.Setup(q => q.IsBlacklistedAsync("123-45-6789", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var rules = new IDenialRule[]
        {
            new StateNyRule(),          // NY applies first
            new BlacklistedSsnRule(mockQuery.Object)
        };
        var engine = new CoreRuleEngine(rules);
        var candidate = new LoanCandidate { Ssn = "123-45-6789", State = "NY" };

        var result = await engine.EvaluateAsync(candidate);

        Assert.True(result.IsFailure);
        Assert.Equal("STATE_NY", result.Error); // Not SSN_BLACKLISTED because NY wins first
    }
}

public class OpenClosedPrincipleTests
{
    /// <summary>
    /// Demonstrates Open/Closed: adding a new rule does not modify existing ones.
    /// </summary>
    [Fact]
    public async Task AddingNewRule_DoesNotModifyExistingRules()
    {
        var mockQuery = new Mock<IBlacklistedSsnQuery>();
        mockQuery.Setup(q => q.IsBlacklistedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Extra fake rule to demonstrate Open/Closed (only applies with a special SSN)
        var fakeRule = new FakeDenialRule("FAKE_REASON");

        var rulesWithFake = new IDenialRule[]
        {
            new StateNyRule(),
            new BlacklistedSsnRule(mockQuery.Object),
            fakeRule
        };

        var engine = new CoreRuleEngine(rulesWithFake);

        // Normal SSN: the fake rule does NOT apply (only applies for "FAKE-SSN")
        var candidate = new LoanCandidate { Ssn = "999-99-9999", State = "CA" };
        var result = await engine.EvaluateAsync(candidate);
        Assert.True(result.IsSuccess);

        // Verify that StateNyRule still works the same as before
        var candidateNy = new LoanCandidate { Ssn = "111-11-1111", State = "NY" };
        var resultNy = await engine.EvaluateAsync(candidateNy);
        Assert.True(resultNy.IsFailure);
        Assert.Equal("STATE_NY", resultNy.Error);

        // Verify that the new fake rule can also trigger if it wanted to
        var candidateFake = new LoanCandidate { Ssn = "FAKE-SSN", State = "CA" };
        var resultFake = await engine.EvaluateAsync(candidateFake);
        Assert.True(resultFake.IsFailure);
        Assert.Equal("FAKE_REASON", resultFake.Error);
    }
}

public class RuleEngineDiTests
{
    /// <summary>
    /// Validates the real Rule Engine wiring: replicates the registration in Program.cs and
    /// verifies that DI injects BOTH rules (in order) via IEnumerable<IDenialRule>.
    /// </summary>
    [Fact]
    public async Task RuleEngine_LoadsAllRegisteredRulesFromDi_InRegistrationOrder()
    {
        var services = new ServiceCollection();
        services.AddScoped<IBlacklistedSsnQuery, StubBlacklistedSsnQuery>();
        services.AddScoped<IDenialRule, StateNyRule>();
        services.AddScoped<IDenialRule, BlacklistedSsnRule>();
        services.AddScoped<CoreRuleEngine>();

        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var engine = scope.ServiceProvider.GetRequiredService<CoreRuleEngine>();

        // NY state + blacklisted SSN: StateNyRule wins (registered first).
        var nyResult = await engine.EvaluateAsync(new LoanCandidate { Ssn = "123-45-6789", State = "NY" });
        Assert.Equal("STATE_NY", nyResult.Error);

        // CA state + blacklisted SSN: StateNyRule no longer applies, BlacklistedSsnRule does.
        var ssnResult = await engine.EvaluateAsync(new LoanCandidate { Ssn = "123-45-6789", State = "CA" });
        Assert.Equal("SSN_BLACKLISTED", ssnResult.Error);
    }

    /// <summary>
    /// Stub that always reports the SSN as blacklisted, so we can resolve
    /// BlacklistedSsnRule from the container without touching PostgreSQL.
    /// </summary>
    private sealed class StubBlacklistedSsnQuery : IBlacklistedSsnQuery
    {
        public Task<bool> IsBlacklistedAsync(string ssn, CancellationToken ct = default)
            => Task.FromResult(true);
    }
}

/// <summary>
/// Fake rule to demonstrate Open/Closed: the principle holds if we can
/// add this class WITHOUT modifying StateNyRule or BlacklistedSsnRule.
/// </summary>
internal sealed class FakeDenialRule : IDenialRule
{
    public FakeDenialRule(string reasonCode) => ReasonCode = reasonCode;
    public string ReasonCode { get; }
    public Task<bool> AppliesAsync(LoanCandidate candidate, CancellationToken ct = default)
        => Task.FromResult(candidate.Ssn == "FAKE-SSN");
}
