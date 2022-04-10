using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using MemCheck.Application.Users;
using SendGrid.Helpers.Mail;

namespace MemCheck.AzureFunctions;

internal sealed class StatsToAdminMailBuilder
{
    #region Fields
    private readonly string azureFunctionName;
    private readonly DateTime azureFunctionStartTime;
    private readonly ImmutableList<EmailAddress> admins;
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
    public StatsToAdminMailBuilder(string azureFunctionName, DateTime azureFunctionStartTime, ImmutableList<EmailAddress> admins, ImmutableList<GetAllUsers.ResultUserModel> allUsers)
    {
        this.azureFunctionName = azureFunctionName;
        this.azureFunctionStartTime = azureFunctionStartTime;
        this.admins = admins;
        this.allUsers = allUsers;
    }
    public string GetMailBody()
    {
        var writer = new StringBuilder()
            .Append("<style>")
            .Append("thead{background-color:darkgray;color:white;}")
            .Append("table{border-width:1px;border-color:green;border-collapse:collapse;}")
            .Append("tr{border-width:1px;border-style:solid;border-color:black;}")
            .Append("td{border-width:1px;border-style:solid;border-color:darkgray;}")
            .Append("tr:nth-child(even){background-color:lightgray;}")
            .Append("</style>")
            .Append("<h1>MemCheck stats</h1>")
            .Append(CultureInfo.InvariantCulture, $"<h2>{allUsers.Count} Users</h2>")
            .Append(GetUsersPart())
            .Append(MailSender.GetMailFooter(azureFunctionName, azureFunctionStartTime, admins));

        return writer.ToString();
    }
}
