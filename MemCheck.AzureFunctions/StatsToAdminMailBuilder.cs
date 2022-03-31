using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
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
        var writer = new StringBuilder();
        writer.Append("<p><table>");
        writer.Append("<thead><tr><th>Name</th><th>Last seen</th></tr></thead>");
        writer.Append("<body>");

        foreach (var user in allUsers)
        {
            writer.Append("<tr style='nth-child(odd) {background: lightgray}'>");
            writer.Append($"<td>{user.UserName}</td><td>{user.LastSeenUtcDate}</td><td>{user.RegistrationUtcDate}</td>");
            writer.Append("</tr>");
        }

        writer.Append("</body>");
        writer.Append("</table></p>");
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
        var writer = new StringBuilder();

        writer.Append("<style>");
        writer.Append("thead{background-color:darkgray;color:white;}");
        writer.Append("table{border-width:1px;border-color:green;border-collapse:collapse;}");
        writer.Append("tr{border-width:1px;border-style:solid;border-color:black;}");
        writer.Append("td{border-width:1px;border-style:solid;border-color:darkgray;}");
        writer.Append("tr:nth-child(even){background-color:lightgray;}");
        writer.Append("</style>");

        writer.Append("<h1>MemCheck stats</h1>");

        writer.Append($"<h2>{allUsers.Count} Users</h2>");
        writer.Append(GetUsersPart());

        writer.Append(MailSender.GetMailFooter(azureFunctionName, azureFunctionStartTime, admins));

        return writer.ToString();
    }
}
