using MemCheck.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MemCheck.Application.Users
{
    public sealed class MemCheckUserManager : UserManager<MemCheckUser>
    {
        public MemCheckUserManager(IUserStore<MemCheckUser> store, IOptions<IdentityOptions> optionsAccessor, IPasswordHasher<MemCheckUser> passwordHasher, IEnumerable<IUserValidator<MemCheckUser>> userValidators, IEnumerable<IPasswordValidator<MemCheckUser>> passwordValidators, ILookupNormalizer keyNormalizer, IdentityErrorDescriber errors, IServiceProvider services, ILogger<UserManager<MemCheckUser>> logger) : base(store, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger)
        {
        }
        public override Task<IdentityResult> DeleteAsync(MemCheckUser user)
        {
            //In MemCheck, a user account is never deleted, but anonymized (with class DeleteUserAccount)
            throw new NotImplementedException("This is not meant to be called");
        }
    }
}
