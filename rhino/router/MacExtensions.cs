using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace Rhino.AI.Router;

internal static class MacExtensions
{

    private const string VERS_STRING = "CFBundleShortVersionString";

    public static bool TryGetVersionInfoFromPlist(string plistPath, [NotNullWhen(true)] out Version? version)
    {
        version = null;
        if (!File.Exists(plistPath)) return false;

        XmlDocument doc = new();
        try
        {
            doc.Load(plistPath);
        }
        catch (Exception e) when (e is XmlException or IOException or UnauthorizedAccessException)
        {
            return false;
        }

        XmlNode? dictNode = doc.GetElementsByTagName("dict")?.Item(0);
        if (dictNode is null) return false;

        for (int i = 0; i < dictNode.ChildNodes.Count; i++)
        {
            XmlNode? node = dictNode.ChildNodes.Item(i);
            if (!string.Equals(node?.InnerText, VERS_STRING)) continue;

            XmlNode? versionNode = dictNode.ChildNodes.Item(i + 1);
            if (!Version.TryParse(versionNode?.InnerText, out Version? v)) continue;
            version = v;
            
            return true;
        }

        return false;
    }

}
