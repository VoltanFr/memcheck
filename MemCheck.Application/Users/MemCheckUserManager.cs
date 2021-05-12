using MemCheck.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;

namespace MemCheck.Application.Users
{
    public sealed class MemCheckUserManager : UserManager<MemCheckUser>
    {
        public MemCheckUserManager(IUserStore<MemCheckUser> store, IOptions<IdentityOptions> optionsAccessor, IPasswordHasher<MemCheckUser> passwordHasher, IEnumerable<IUserValidator<MemCheckUser>> userValidators, IEnumerable<IPasswordValidator<MemCheckUser>> passwordValidators, ILookupNormalizer keyNormalizer, IdentityErrorDescriber errors, IServiceProvider services, ILogger<UserManager<MemCheckUser>> logger) : base(store, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger)
        {
        }
    }
}
