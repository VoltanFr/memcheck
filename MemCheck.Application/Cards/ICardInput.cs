using System;
using System.Collections.Generic;

namespace MemCheck.Application.Cards;

public interface ICardInput : IRequest
{
    Guid VersionCreatorId { get; }
    string FrontSide { get; }
    string BackSide { get; }
    string AdditionalInfo { get; }
    string References { get; }
    Guid LanguageId { get; }
    IEnumerable<Guid> Tags { get; }
    IEnumerable<Guid> UsersWithVisibility { get; }
    string VersionDescription { get; }
}
