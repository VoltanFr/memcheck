using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using MemCheck.Application.Users;

namespace MemCheck.AzureFunctions;

internal sealed class StatsToAdminMailBuilder
{
    #region Fields
    private readonly ImmutableList<GetAllUsers.ResultUserModel> allUsers;
    #endregion
    #region Private methods
    private string GetUsersPart()
    {
        var writer = new StringBuilder()
            .Append("<p><table>")
            .Append("<thead><tr><th>Name</th><th>Last seen</th></tr></thead>")
            .Append("<body>");

        foreach (var user in allUsers)
        {
            writer = writer
                .Append("<tr style='nth-child(odd) {background: lightgray}'>")
                .Append(CultureInfo.InvariantCulture, $"<td>{user.UserName}</td><td>{user.LastSeenUtcDate}</td><td>{user.RegistrationUtcDate}</td>")
                .Append("</tr>");
        }

        writer = writer
            .Append("</body>")
            .Append("</table></p>");

        return writer.ToString();
    }
    #endregion
    public StatsToAdminMailBuilder(ImmutableList<GetAllUsers.ResultUserModel> allUsers)
    {
        this.allUsers = allUsers;
    }
    public string GetMailBody()
    {
        return $"<h2>{allUsers.Count} Users</h2>{GetUsersPart()}";
    }
}
