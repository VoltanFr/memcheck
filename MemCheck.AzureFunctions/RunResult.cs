using System.Text;

namespace MemCheck.AzureFunctions;

public sealed record RunResult(string MailSubject, StringBuilder MailBodyMainPart);
