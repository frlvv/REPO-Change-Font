using System;
using System.IO;
using System.Text.RegularExpressions;

// Reads Steam's local record only; Steam remains responsible for downloading updates.
static class SteamGameUpdate {
    public static bool IsPending(string game) {
        try {
            if (String.IsNullOrEmpty(game)) return false;
            var dir = new DirectoryInfo(Path.GetFullPath(game));
            if (dir.Parent == null || !String.Equals(dir.Parent.Name, "common", StringComparison.OrdinalIgnoreCase) || dir.Parent.Parent == null || !String.Equals(dir.Parent.Parent.Name, "steamapps", StringComparison.OrdinalIgnoreCase)) return false;
            string manifest = Path.Combine(dir.Parent.Parent.FullName, "appmanifest_3241660.acf");
            string text = File.ReadAllText(manifest);
            string appId = Value(text, "appid"), installDir = Value(text, "installdir"), state = Value(text, "StateFlags");
            uint flags;
            return appId == "3241660" && String.Equals(installDir, dir.Name, StringComparison.OrdinalIgnoreCase) && UInt32.TryParse(state, out flags) && (flags & 2) != 0;
        } catch (Exception) { return false; }
    }
    static string Value(string text, string key) {
        var match = Regex.Match(text, "(?im)^\\s*\"" + Regex.Escape(key) + "\"\\s*\"([^\"]*)\"");
        return match.Success ? match.Groups[1].Value : null;
    }
}
