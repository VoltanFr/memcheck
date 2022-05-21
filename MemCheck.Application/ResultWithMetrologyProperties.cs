using System.Collections.Generic;

namespace MemCheck.Application;

public class ResultWithMetrologyProperties<TResult>
{
    #region Fields
    private readonly (string key, string value)[] properties;
    #endregion
    public ResultWithMetrologyProperties(TResult actualResult, params (string key, string value)[] properties)
    {
        ActualResult = actualResult;
        this.properties = properties;
    }
    public TResult ActualResult { get; }
    public IEnumerable<(string key, string value)> Properties => properties;
}

