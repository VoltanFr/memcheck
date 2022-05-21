using MemCheck.Application.QueryValidation;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Notifiying;

public sealed class SetSearchSubscriptionName : RequestRunner<SetSearchSubscriptionName.Request, SetSearchSubscriptionName.Result>
{
    public SetSearchSubscriptionName(CallContext callContext) : base(callContext)
    {
    }
    protected override async Task<ResultWithMetrologyProperties<Result>> DoRunAsync(Request request)
    {
        var subscription = await DbContext.SearchSubscriptions.Where(s => s.Id == request.SubscriptionId).SingleAsync();
        subscription.Name = request.Name;
        await DbContext.SaveChangesAsync();
        return new ResultWithMetrologyProperties<Result>(new Result(), ("Name", request.Name.ToString()), IntMetric("NameLength", request.Name.Length));
    }
    #region Request & Result
    public sealed class Request : IRequest
    {
        public const int MinNameLength = 3;
        public const int MaxNameLength = 36;
        public Request(Guid userId, Guid subscriptionId, string name)
        {
            UserId = userId;
            SubscriptionId = subscriptionId;
            Name = name.Trim();
        }
        public Guid UserId { get; }
        public Guid SubscriptionId { get; }
        public string Name { get; }
        public async Task CheckValidityAsync(CallContext callContext)
        {
            QueryValidationHelper.CheckNotReservedGuid(UserId);
            QueryValidationHelper.CheckNotReservedGuid(SubscriptionId);
            if (Name.Length < MinNameLength)
                throw new RequestInputException($"Name '{Name}' is too short, must be between {MinNameLength} and {MaxNameLength} chars long, is {Name.Length}");
            if (Name.Length > MaxNameLength)
                throw new RequestInputException($"Name '{Name}' is too long, must be between {MinNameLength} and {MaxNameLength} chars long, is {Name.Length}");
            var subscription = await callContext.DbContext.SearchSubscriptions.Where(s => s.Id == SubscriptionId).SingleOrDefaultAsync();
            if (subscription == null)
                throw new RequestInputException("Subscription not found");
            if (subscription.UserId != UserId)
                throw new RequestInputException("User not owner of subscription");
        }
    }
    public sealed record Result();
    #endregion
}
