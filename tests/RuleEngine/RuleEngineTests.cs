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
    [InlineData("ny", false)] // Case sensitive: solo mayúsculas
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
        // Si ambas reglas aplican, gana la primera (StateNY en este caso por orden)
        var mockQuery = new Mock<IBlacklistedSsnQuery>();
        mockQuery.Setup(q => q.IsBlacklistedAsync("123-45-6789", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var rules = new IDenialRule[]
        {
            new StateNyRule(),          // NY aplica primero
            new BlacklistedSsnRule(mockQuery.Object)
        };
        var engine = new CoreRuleEngine(rules);
        var candidate = new LoanCandidate { Ssn = "123-45-6789", State = "NY" };

        var result = await engine.EvaluateAsync(candidate);

        Assert.True(result.IsFailure);
        Assert.Equal("STATE_NY", result.Error); // No SSN_BLACKLISTED porque NY gana primero
    }
}

public class OpenClosedPrincipleTests
{
    /// <summary>
    /// Demuestra Open/Closed: agregar una nueva regla NO modifica las existentes.
    /// </summary>
    [Fact]
    public async Task AddingNewRule_DoesNotModifyExistingRules()
    {
        var mockQuery = new Mock<IBlacklistedSsnQuery>();
        mockQuery.Setup(q => q.IsBlacklistedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Regla extra fake para demostrar Open/Closed (solo aplica con SSN especial)
        var fakeRule = new FakeDenialRule("FAKE_REASON");

        var rulesWithFake = new IDenialRule[]
        {
            new StateNyRule(),
            new BlacklistedSsnRule(mockQuery.Object),
            fakeRule
        };

        var engine = new CoreRuleEngine(rulesWithFake);

        // SSN normal: la regla fake NO aplica (solo aplica para "FAKE-SSN")
        var candidate = new LoanCandidate { Ssn = "999-99-9999", State = "CA" };
        var result = await engine.EvaluateAsync(candidate);
        Assert.True(result.IsSuccess);

        // Verificar que StateNyRule sigue funcionando igual que antes
        var candidateNy = new LoanCandidate { Ssn = "111-11-1111", State = "NY" };
        var resultNy = await engine.EvaluateAsync(candidateNy);
        Assert.True(resultNy.IsFailure);
        Assert.Equal("STATE_NY", resultNy.Error);

        // Verificar que la nueva regla fake también puede activar si quisiera
        var candidateFake = new LoanCandidate { Ssn = "FAKE-SSN", State = "CA" };
        var resultFake = await engine.EvaluateAsync(candidateFake);
        Assert.True(resultFake.IsFailure);
        Assert.Equal("FAKE_REASON", resultFake.Error);
    }
}

public class RuleEngineDiTests
{
    /// <summary>
    /// Valida el wiring real del Rule Engine: replica el registro de Program.cs y
    /// comprueba que el DI inyecta AMBAS reglas (en orden) vía IEnumerable<IDenialRule>.
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

        // Estado NY + SSN en blacklist: gana StateNyRule (registrada primero).
        var nyResult = await engine.EvaluateAsync(new LoanCandidate { Ssn = "123-45-6789", State = "NY" });
        Assert.Equal("STATE_NY", nyResult.Error);

        // Estado CA + SSN en blacklist: ya no aplica StateNyRule, aplica BlacklistedSsnRule.
        var ssnResult = await engine.EvaluateAsync(new LoanCandidate { Ssn = "123-45-6789", State = "CA" });
        Assert.Equal("SSN_BLACKLISTED", ssnResult.Error);
    }

    /// <summary>
    /// Stub que siempre responde que el SSN está en blacklist, para poder resolver
    /// BlacklistedSsnRule desde el contenedor sin tocar PostgreSQL.
    /// </summary>
    private sealed class StubBlacklistedSsnQuery : IBlacklistedSsnQuery
    {
        public Task<bool> IsBlacklistedAsync(string ssn, CancellationToken ct = default)
            => Task.FromResult(true);
    }
}

/// <summary>
/// Regla fake para demostrar Open/Closed: el principio se cumple si podemos
/// agregar esta clase SIN modificar StateNyRule ni BlacklistedSsnRule.
/// </summary>
internal sealed class FakeDenialRule : IDenialRule
{
    public FakeDenialRule(string reasonCode) => ReasonCode = reasonCode;
    public string ReasonCode { get; }
    public Task<bool> AppliesAsync(LoanCandidate candidate, CancellationToken ct = default)
        => Task.FromResult(candidate.Ssn == "FAKE-SSN");
}
