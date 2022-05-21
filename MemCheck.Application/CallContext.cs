using MemCheck.Application.QueryValidation;
using MemCheck.Database;

namespace MemCheck.Application;

public sealed record CallContext(MemCheckDbContext DbContext, IMemCheckTelemetryClient TelemetryClient, ILocalized Localized, IRoleChecker RoleChecker)
{
}
