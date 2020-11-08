using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MemCheck.Database;
using MemCheck.Domain;

namespace MemCheck.Application
{
    public sealed class SetUserUILanguage
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        private readonly HashSet<string> supportedUILanguages;
        #endregion
        public SetUserUILanguage(MemCheckDbContext dbContext, IEnumerable<string> supportedUILanguages)
        {
            this.dbContext = dbContext;
            this.supportedUILanguages = new HashSet<string>(supportedUILanguages);
        }
        public void Run(MemCheckUser user, string cultureName)
        {
            if (!supportedUILanguages.Contains(cultureName))
                throw new InvalidProgramException($"Invalid culture '{cultureName}'");
            user.UILanguage = cultureName;
            dbContext.SaveChanges();
        }

    }
}
