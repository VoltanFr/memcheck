using MemCheck.Database;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Loading
{
    internal sealed class CardRegistrationsLoader
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        public CardRegistrationsLoader(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public ImmutableDictionary<Guid, bool> RunForCardIds(Guid userId, IEnumerable<Guid> cardIds)
        {
            //We don't care about erroneous inputs: this is the client code's responsibility (this class is internal)
            var notifs = dbContext.CardNotifications.Where(notif => notif.UserId == userId && cardIds.Contains(notif.CardId)).Select(notif => notif.CardId).ToImmutableHashSet();
            return cardIds.Select(cardId => new KeyValuePair<Guid, bool>(cardId, notifs.Contains(cardId))).ToImmutableDictionary();
        }
    }
}