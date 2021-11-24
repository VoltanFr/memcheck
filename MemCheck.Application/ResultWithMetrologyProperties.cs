namespace MemCheck.Application
{
    public class ResultWithMetrologyProperties<TResult>
    {
        #region Fields
        private readonly TResult actualResult;
        private readonly (string key, string value)[] properties;
        #endregion
        public ResultWithMetrologyProperties(TResult actualResult, params (string key, string value)[] properties)
        {
            this.actualResult = actualResult;
            this.properties = properties;
        }
        public TResult ActualResult => actualResult;
        public (string key, string value)[] Properties => properties;
    }

}
