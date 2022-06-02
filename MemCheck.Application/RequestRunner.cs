using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application;

public abstract class RequestRunner<TRequest, TResult> : ClassWithMetrics where TRequest : IRequest
{
    #region Fields
    #endregion
    protected RequestRunner(CallContext callContext)
    {
        CallContext = callContext;
    }
    protected abstract Task<ResultWithMetrologyProperties<TResult>> DoRunAsync(TRequest request);
    public async Task<TResult> RunAsync(TRequest request)
    {
        var checkValidityChrono = Stopwatch.StartNew();
        await request.CheckValidityAsync(CallContext);
        checkValidityChrono.Stop();

        var runChrono = Stopwatch.StartNew();
        var result = await DoRunAsync(request);
        runChrono.Stop();

        var metrologyProperties = result.Properties.ToList();
        metrologyProperties.Add(DurationMetric("CheckValidityDuration", checkValidityChrono.Elapsed));
        metrologyProperties.Add(DurationMetric("RunDuration", runChrono.Elapsed));

        CallContext.TelemetryClient.TrackEvent(GetType().Name, metrologyProperties.ToArray());

        return result.ActualResult;
    }
    protected MemCheckDbContext DbContext => CallContext.DbContext;
    protected ILocalized Localized => CallContext.Localized;
    protected IRoleChecker RoleChecker => CallContext.RoleChecker;
    protected CallContext CallContext { get; }
}

