using MemCheck.Domain;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Users;

public sealed class MemCheckUserValidator : UserValidator<MemCheckUser>
{
    public const int MinUserNameLength = 3;
    public const int MaxUserNameLength = 50;
    public const string ForbiddenCharactersInUserName = "\t";
    public const string UserNameNotTrimmedErrorCode = "UserNameNotTrimmed";
    public const string UserNameBadLengthErrorCode = "BadUserNameLength";
    public const string EmailNotTrimmedErrorCode = "EMailNotTrimmed";
    public const string UserNameNameDoesNotStartWithALetter = "UserNameNameDoesNotStartWithALetter";
    public const string UserNameContainsControlChar = "UserNameContainsControlChar";
    public const string UserNameContainsForbiddenChar = "UserNameContainsForbiddenChar";
    public override async Task<IdentityResult> ValidateAsync(UserManager<MemCheckUser> manager, MemCheckUser user)
    {
        var errors = new List<IdentityError>();
        await ValidateUserName(manager, user, errors);
        await ValidateEmail(manager, user, errors);
        return errors.Count > 0 ? IdentityResult.Failed(errors.ToArray()) : IdentityResult.Success;
    }
    private async Task ValidateUserName(UserManager<MemCheckUser> manager, MemCheckUser user, List<IdentityError> errors)
    {
        if (user.GetUserName().Trim() != user.UserName)
        {
            errors.Add(new IdentityError() { Code = UserNameNotTrimmedErrorCode, Description = "User name must not start or end with blank chars" });
            return; // This is a bug, the client code should have trimmed: let's exit
        }

        if (user.UserName.Length is > MaxUserNameLength or < MinUserNameLength)
            errors.Add(new IdentityError() { Code = UserNameBadLengthErrorCode, Description = $"User name must contain between {MinUserNameLength} and {MaxUserNameLength} chars" });

        var containsControlChar = user.UserName.Any(c => char.IsControl(c));
        if (containsControlChar)
            errors.Add(new IdentityError() { Code = UserNameContainsControlChar });

        var firstInvalidChar = user.UserName.FirstOrDefault(c => ForbiddenCharactersInUserName.Contains(c, StringComparison.Ordinal));
        if (firstInvalidChar != '\0')
            errors.Add(new IdentityError() { Code = UserNameContainsForbiddenChar, Description = firstInvalidChar.ToString() });

        if (!char.IsLetter(user.UserName[0]))
            errors.Add(new IdentityError() { Code = UserNameNameDoesNotStartWithALetter });

        var userFromDb = await manager.FindByNameAsync(user.UserName);
        if (userFromDb != null)
        {
            var userIdFromDb = await manager.GetUserIdAsync(userFromDb);
            var thisUserId = await manager.GetUserIdAsync(user);
            if (!string.Equals(userIdFromDb, thisUserId, StringComparison.Ordinal))
                errors.Add(Describer.DuplicateUserName(user.UserName));
        }
    }
    private async Task ValidateEmail(UserManager<MemCheckUser> manager, MemCheckUser user, List<IdentityError> errors)
    {
        var email = await manager.GetEmailAsync(user);
        if (email == null)
        {
            errors.Add(new IdentityError() { Code = nameof(IdentityErrorDescriber.InvalidEmail), Description = "Email address must be provided" });
            return;
        }
        if (email.Trim() != email)
        {
            errors.Add(new IdentityError() { Code = EmailNotTrimmedErrorCode, Description = "Email address must not start or end with blank chars" });
            return; // This is a bug, the client code should have trimmed: let's exit
        }
        if (string.IsNullOrWhiteSpace(email))
        {
            errors.Add(Describer.InvalidEmail(email));
            return;
        }
        if (!new EmailAddressAttribute().IsValid(email))
        {
            errors.Add(Describer.InvalidEmail(email));
            return;
        }
    }
}
