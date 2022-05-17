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
    private readonly ImmutableList<GetAllUsers.ResultUserModel> allUsers;
    private readonly GetRecentDemoUses.Result recentDemos;
    private readonly ImmutableDictionary<Guid, string> tagNames;
    #endregion
    #region Private methods
    private string GetUsersPart()
    {
        var writer = new StringBuilder();

        writer.Append(CultureInfo.InvariantCulture, $"<h1>{allUsers.Count} Users</h1>")
            .Append("<p><table>")
            .Append("<thead><tr><th>Name</th><th>Last seen</th></tr></thead>")
            .Append("<tbody>");

        foreach (var user in allUsers)
            writer = writer
                .Append("<tr style='nth-child(odd) {background: lightgray}'>")
                .Append(CultureInfo.InvariantCulture, $"<td>{user.UserName}</td><td>{user.LastSeenUtcDate}</td><td>{user.RegistrationUtcDate}</td>")
                .Append("</tr>");

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
                .Append("<tr style='nth-child(odd) {background: lightgray}'>")
                .Append(CultureInfo.InvariantCulture, $"<td>{recentDemo.DownloadUtcDate}</td><td>{tagNames[recentDemo.TagId]}</td><td>{recentDemo.CountOfCardsReturned}</td>")
                .Append("</tr>");
        }

        writer = writer
            .Append("</tbody>")
            .Append("</table></p>");

        return writer.ToString();
    }
    #endregion
    public StatsToAdminMailBuilder(ImmutableList<GetAllUsers.ResultUserModel> allUsers, GetRecentDemoUses.Result recentDemos, ImmutableDictionary<Guid, string> tagNames)
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
