using System;
using System.Collections.Generic;

namespace MemCheck.Application.Cards
{
    public interface ICardInput : IRequest
    {
        public Guid VersionCreatorId { get; }
        public string FrontSide { get; }
        public IEnumerable<Guid> FrontSideImageList { get; }
        public string BackSide { get; }
        public IEnumerable<Guid> BackSideImageList { get; }
        public string AdditionalInfo { get; }
        public IEnumerable<Guid> AdditionalInfoImageList { get; }
        public Guid LanguageId { get; }
        public IEnumerable<Guid> Tags { get; }
        public IEnumerable<Guid> UsersWithVisibility { get; }
        public string VersionDescription { get; }
    }
}