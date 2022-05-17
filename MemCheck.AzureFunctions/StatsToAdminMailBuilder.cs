using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using MemCheck.Application.Cards;
using MemCheck.Application.Users;

namespace MemCheck.AzureFunctions;

internal sealed class StatsToAdminMailBuilder
{
    #region Fields
    private readonly ImmutableList<GetAllUsersStats.ResultUserModel> allUsers;
    private readonly GetRecentDemoUses.Result recentDemos;
    private readonly ImmutableDictionary<Guid, string> tagNames;
    #endregion
    #region Private methods
    private string GetUsersPart()
    {
        var writer = new StringBuilder();

        writer.Append(CultureInfo.InvariantCulture, $"<h1>{allUsers.Count} Users</h1>")
            .Append("<p><table>")
            .Append(@"<thead><tr><th scope=""col"">Name</th><th scope=""col"">Last seen</th><th scope=""col"">Registration</th><th scope=""col"">Decks</th></tr></thead>")
            .Append("<tbody>");

        foreach (var user in allUsers)
        {
            writer = writer
                .Append("<tr>")
                .Append(CultureInfo.InvariantCulture, $"<td>{user.UserName}</td>")
                .Append(CultureInfo.InvariantCulture, $"<td>{user.LastSeenUtcDate}</td>")
                .Append(CultureInfo.InvariantCulture, $"<td>{user.RegistrationUtcDate}</td>")
                .Append("<td><ul>");

            foreach (var deck in user.Decks)
                writer = writer.Append(CultureInfo.InvariantCulture, $"<li>{deck.Name}: {deck.CardCount} cards</li>");

            writer = writer
                .Append("</ul></td></tr>");
        }

        writer = writer
            .Append("</tbody>")
            .Append("</table></p>");

        return writer.ToString();
    }
    private string GetRecentDemosPart()
    {
        var writer = new StringBuilder();

        writer.Append(CultureInfo.InvariantCulture, $"<h1>{recentDemos.Entries.Length} demos in the last {recentDemos.DayCount} days</h1>")
            .Append("<p><table>")
            .Append("<thead><tr><th>Date</th><th>Tag</th><th>Card count</th></tr></thead>")
            .Append("<tbody>");

        foreach (var recentDemo in recentDemos.Entries)
        {
            writer = writer
                .Append("<tr>")
                .Append(CultureInfo.InvariantCulture, $"<td>{recentDemo.DownloadUtcDate}</td><td>{tagNames[recentDemo.TagId]}</td><td>{recentDemo.CountOfCardsReturned}</td>")
                .Append("</tr>");
        }

        writer = writer
            .Append("</tbody>")
            .Append("</table></p>");

        return writer.ToString();
    }
    #endregion
    public StatsToAdminMailBuilder(ImmutableList<GetAllUsersStats.ResultUserModel> allUsers, GetRecentDemoUses.Result recentDemos, ImmutableDictionary<Guid, string> tagNames)
    {
        this.allUsers = allUsers;
        this.recentDemos = recentDemos;
        this.tagNames = tagNames;
    }
    public string GetMailBody()
    {
        return GetUsersPart() + GetRecentDemosPart();
    }
}
