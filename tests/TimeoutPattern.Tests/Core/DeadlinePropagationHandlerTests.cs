using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using TimeoutPattern.Core.Budgets;
using TimeoutPattern.Core.Configuration;
using TimeoutPattern.Infrastructure.Http;

namespace TimeoutPattern.Tests.Core;

public sealed class DeadlinePropagationHandlerTests
{
    [Fact]
    public async Task Advertises_remaining_budget_minus_network_allowance()
    {
        var clock = new FakeTimeProvider();
        var budgets = new TimeoutBudgetAccessor
        {
            Current = TimeoutBudget.ForRequest("request", TimeSpan.FromMilliseconds(800), clock),
        };
        clock.Advance(TimeSpan.FromMilliseconds(300));

        var options = new TimeoutOptions { Propagation = { NetworkAllowance = TimeSpan.FromMilliseconds(15) } };
        var capture = new CapturingHandler();
        using var invoker = new HttpMessageInvoker(new DeadlinePropagationHandler(budgets, new StaticMonitor(options)) { InnerHandler = capture });

        await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://downstream/"), CancellationToken.None);

        Assert.Equal("485", capture.Header(options.Propagation.HeaderName)); // 800 − 300 − 15
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        private HttpRequestMessage? _request;

        public string? Header(string name) =>
            _request!.Headers.TryGetValues(name, out var values) ? values.Single() : null;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _request = request;
            return Task.FromResult(new HttpResponseMessage());
        }
    }

    private sealed class StaticMonitor(TimeoutOptions value) : IOptionsMonitor<TimeoutOptions>
    {
        public TimeoutOptions CurrentValue => value;

        public TimeoutOptions Get(string? name) => value;

        public IDisposable? OnChange(Action<TimeoutOptions, string?> listener) => null;
    }
}
