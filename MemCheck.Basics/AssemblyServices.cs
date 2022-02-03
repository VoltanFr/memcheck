using System.Reflection;

namespace MemCheck.Basics
{
    public static class AssemblyServices
    {
        public static string GetDisplayInfoForAssembly(Assembly? a)
        {
            if (a == null)
                return "Unknown";

            var informationalVersionAttribute = a.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            var version = informationalVersionAttribute == null ? "Unknown" : informationalVersionAttribute.InformationalVersion;
            return a.GetName().Name + ' ' + version;
        }
    }
}
