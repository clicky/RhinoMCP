using System.Xml;

namespace RhinoAI.Router;

internal static class MacExtensions
{

    private const string VERS_STRING = "CFBundleShortVersionString";

    public static bool TryGetVersionInfoFromPlist(string plistPath, out Version version)
    {
        version = default!;
        if (!File.Exists(plistPath)) return false;

        XmlDocument doc = new();
        doc.Load(plistPath);

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
