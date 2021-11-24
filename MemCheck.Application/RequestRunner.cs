using MemCheck.Database;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application
{
    public abstract class RequestRunner<TRequest, TResult> where TRequest : IRequest
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
            await request.CheckValidityAsync(callContext.DbContext);
            checkValidityChrono.Stop();

            var runChrono = Stopwatch.StartNew();
            var result = await DoRunAsync(request);
            runChrono.Stop();

            var metrologyProperties = result.Properties.ToList();
            metrologyProperties.Add(("CheckValidityDuration", checkValidityChrono.ElapsedMilliseconds.ToString()));
            metrologyProperties.Add(("RunDuration", runChrono.ElapsedMilliseconds.ToString()));

            callContext.TelemetryClient.TrackEvent(GetType().Name, metrologyProperties.ToArray());

            return result.ActualResult;
        }
        protected MemCheckDbContext DbContext => callContext.DbContext;
    }

}
