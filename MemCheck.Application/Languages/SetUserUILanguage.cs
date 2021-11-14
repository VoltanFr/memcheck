using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace MemCheck.Application.Languages
{
    public sealed class SetUserUILanguage
    {
        #region Fields
        private readonly CallContext callContext;
        #endregion
        public SetUserUILanguage(CallContext callContext)
        {
            this.callContext = callContext;
        }
        public async Task RunAsync(Request request)
        {
            await request.CheckValidityAsync(callContext.DbContext);
            var user = await callContext.DbContext.Users.SingleAsync(user => user.Id == request.UserId);
            user.UILanguage = request.CultureName;
            callContext.DbContext.SaveChanges();
            callContext.TelemetryClient.TrackEvent("SetUserUILanguage", ("CultureName", request.CultureName));
        }
        #region Request type
        public sealed record Request(Guid UserId, string CultureName)
        {
            public const int MinNameLength = 5;
            public const int MaxNameLength = 5;
            public async Task CheckValidityAsync(MemCheckDbContext dbContext)
            {
                await QueryValidationHelper.CheckUserExistsAsync(dbContext, UserId);
                if (CultureName != CultureName.Trim())
                    throw new InvalidOperationException("Invalid Name: not trimmed");
                if (CultureName.Length < MinNameLength || CultureName.Length > MaxNameLength)
                    throw new InvalidOperationException($"Invalid culture name '{CultureName}'");
            }
        }
        #endregion
    }
}
