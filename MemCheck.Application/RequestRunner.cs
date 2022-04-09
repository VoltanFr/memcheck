using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application
{
    public abstract class ClassWithMetrics
    {
        public static (string key, string value) IntMetric(string key, int value)
        {
            return (key, value.ToString(CultureInfo.InvariantCulture));
        }
        protected static (string key, string value) DoubleMetric(string key, double value)
        {
            return (key, value.ToString(CultureInfo.InvariantCulture));
        }
        protected static (string key, string value) DurationMetric(string key, TimeSpan value)
        {
            return (key, value.TotalMilliseconds.ToString(CultureInfo.InvariantCulture));
        }
    }
    public abstract class RequestRunner<TRequest, TResult> : ClassWithMetrics where TRequest : IRequest
    {
        #region Fields
        private readonly CallContext callContext;
        #endregion
        protected RequestRunner(CallContext callContext)
        {
            this.callContext = callContext;
        }
        protected abstract Task<ResultWithMetrologyProperties<TResult>> DoRunAsync(TRequest request);
        public async Task<TResult> RunAsync(TRequest request)
        {
            var checkValidityChrono = Stopwatch.StartNew();
            await request.CheckValidityAsync(callContext);
            checkValidityChrono.Stop();

            var runChrono = Stopwatch.StartNew();
            var result = await DoRunAsync(request);
            runChrono.Stop();

            var metrologyProperties = result.Properties.ToList();
            metrologyProperties.Add(DurationMetric("CheckValidityDuration", checkValidityChrono.Elapsed));
            metrologyProperties.Add(DurationMetric("RunDuration", runChrono.Elapsed));

            callContext.TelemetryClient.TrackEvent(GetType().Name, metrologyProperties.ToArray());

            return result.ActualResult;
        }
        protected MemCheckDbContext DbContext => callContext.DbContext;
        protected ILocalized Localized => callContext.Localized;
        protected IRoleChecker RoleChecker => callContext.RoleChecker;
    }

}
