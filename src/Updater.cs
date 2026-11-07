using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Threading;
using System.Collections.Generic;
using System.Web.Script.Serialization;

static class AppUpdater {
    const string Asset = "R.E.P.O.Change.Font.exe";
    const string Sums = "SHA256SUMS.txt";
    const string Api = "https://api.github.com/repos/frlvv/REPO-Change-Font/releases/latest";

    public sealed class Release {
        public string Version, ExeUrl, SumsUrl;
    }

    public sealed class DownloadedUpdate {
        public string Path, ExpectedHash;
    }

    sealed class TimedClient : WebClient {
        protected override WebRequest GetWebRequest(Uri address) {
            var request = base.GetWebRequest(address);
            request.Timeout = 15000;
            var http = request as HttpWebRequest;
            if (http != null) http.ReadWriteTimeout = 15000;
            return request;
        }
    }

    static WebClient Client() {
        ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
        var client = new TimedClient();
        client.Headers[HttpRequestHeader.UserAgent] = "REPO-Change-Font-Updater";
        return client;
    }

    static Version ParseVersion(string text) {
        Version version;
        return Version.TryParse((text ?? "").TrimStart('v', 'V'), out version) ? version : null;
    }

    public static Release Check() {
        try { return CheckApi(); }
        catch (WebException) { return CheckRedirect(); }
    }

    static Release CheckRedirect() {
        ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
        var request = (HttpWebRequest)WebRequest.Create("https://github.com/frlvv/REPO-Change-Font/releases/latest");
        request.UserAgent = "REPO-Change-Font-Updater";
        request.Timeout = 15000;
        request.ReadWriteTimeout = 15000;
        using (var response = (HttpWebResponse)request.GetResponse()) {
            var uri = response.ResponseUri;
            const string prefix = "/frlvv/REPO-Change-Font/releases/tag/";
            if (uri.Scheme != "https" || !uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) ||
                !uri.AbsolutePath.StartsWith(prefix, StringComparison.Ordinal)) return null;
            string tag = Uri.UnescapeDataString(uri.AbsolutePath.Substring(prefix.Length));
            if (!System.Text.RegularExpressions.Regex.IsMatch(tag, @"^[vV]?\d+\.\d+\.\d+(\.\d+)?$")) return null;
            var version = ParseVersion(tag);
            var current = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            if (version == null || version <= current) return null;
            string root = "https://github.com/frlvv/REPO-Change-Font/releases/download/" + Uri.EscapeDataString(tag) + "/";
            return new Release { Version = tag, ExeUrl = root + Uri.EscapeDataString(Asset), SumsUrl = root + Sums };
        }
    }

    static Release CheckApi() {
        using (var client = Client()) {
            var data = new JavaScriptSerializer().DeserializeObject(client.DownloadString(Api)) as Dictionary<string, object>;
            if (data == null || (data.ContainsKey("draft") && Convert.ToBoolean(data["draft"])) ||
                (data.ContainsKey("prerelease") && Convert.ToBoolean(data["prerelease"]))) return null;
            string tag = Convert.ToString(data["tag_name"]);
            var latest = ParseVersion(tag);
            var current = ParseVersion(System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
            if (latest == null || current == null || latest <= current) return null;
            var result = new Release { Version = tag };
            foreach (object item in (object[])data["assets"]) {
                var asset = item as Dictionary<string, object>;
                if (asset == null) continue;
                string name = Convert.ToString(asset["name"]);
                string url = Convert.ToString(asset["browser_download_url"]);
                Uri uri;
                if (!Uri.TryCreate(url, UriKind.Absolute, out uri) || uri.Scheme != "https" ||
                    !uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) ||
                    !uri.AbsolutePath.StartsWith("/frlvv/REPO-Change-Font/releases/download/", StringComparison.Ordinal)) continue;
                if (name == Asset) result.ExeUrl = url;
                else if (name == Sums) result.SumsUrl = url;
            }
            return result.ExeUrl != null && result.SumsUrl != null ? result : null;
        }
    }

    static string Hash(string path) {
        using (var stream = File.OpenRead(path))
        using (var sha = SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
    }

    public static DownloadedUpdate Download(Release release) {
        using (var client = Client()) {
            string expected = null;
            foreach (string line in client.DownloadString(release.SumsUrl).Split('\n')) {
                string row = line.Trim();
                if (row.Length > 66 && row.Substring(64).TrimStart().TrimStart('*') == Asset) {
                    string hash = row.Substring(0, 64);
                    if (System.Text.RegularExpressions.Regex.IsMatch(hash, "^[a-fA-F0-9]{64}$")) expected = hash.ToLowerInvariant();
                }
            }
            if (expected == null) throw new InvalidDataException("Checksum missing.");
            string dir = Path.Combine(Path.GetTempPath(), "REPO-Change-Font-update-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            string file = Path.Combine(dir, Asset);
            try {
                client.DownloadFile(release.ExeUrl, file);
                if (Hash(file) != expected) throw new InvalidDataException("Checksum mismatch.");
                var actual = ParseVersion(FileVersionInfo.GetVersionInfo(file).FileVersion);
                var claimed = ParseVersion(release.Version);
                if (actual == null || claimed == null || actual.Major != claimed.Major ||
                    actual.Minor != claimed.Minor || actual.Build != claimed.Build) throw new InvalidDataException("Version mismatch.");
                return new DownloadedUpdate { Path = file, ExpectedHash = expected };
            } catch {
                try { Directory.Delete(dir, true); } catch { }
                throw;
            }
        }
    }

    static string Quote(string text) { return "\"" + text.Replace("\"", "\\\"") + "\""; }

    public static void Install(DownloadedUpdate update) {
        string staged = update.Path;
        string current = System.Reflection.Assembly.GetExecutingAssembly().Location;
        string helper = Path.Combine(Path.GetDirectoryName(staged), "update-helper.exe");
        File.Copy(current, helper, true);
        var start = new ProcessStartInfo(helper, "--install-update " + Process.GetCurrentProcess().Id + " " +
            Quote(current) + " " + Quote(staged) + " " + update.ExpectedHash) { UseShellExecute = true };
        Process.Start(start);
    }

    public static bool HandleInstall(string[] args) {
        if (args.Length != 5 || args[0] != "--install-update") return false;
        string target = args[2], staged = args[3], expected = args[4];
        try {
            string helper = System.Reflection.Assembly.GetExecutingAssembly().Location;
            if (!string.Equals(Path.GetFileName(helper), "update-helper.exe", StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(Path.GetFullPath(staged), Path.Combine(Path.GetDirectoryName(helper), Asset), StringComparison.OrdinalIgnoreCase) ||
                !System.Text.RegularExpressions.Regex.IsMatch(expected, "^[a-f0-9]{64}$")) throw new IOException("Invalid update request.");
            int pid = int.Parse(args[1]);
            try {
                using (var parent = Process.GetProcessById(pid)) {
                    if (!string.Equals(Path.GetFullPath(parent.MainModule.FileName), Path.GetFullPath(target), StringComparison.OrdinalIgnoreCase))
                        throw new IOException("Invalid update target.");
                    if (!parent.WaitForExit(30000)) throw new IOException("The application did not close.");
                }
            } catch (ArgumentException) { }
            if (!File.Exists(staged) || Hash(staged) != expected) throw new IOException("Update verification failed.");
            string backup = target + ".update-backup-" + Guid.NewGuid().ToString("N");
            string sibling = target + ".update-new-" + Guid.NewGuid().ToString("N");
            bool siblingCreated = false;
            try {
                using (var input = File.OpenRead(staged))
                using (var output = new FileStream(sibling, FileMode.CreateNew, FileAccess.Write, FileShare.None)) {
                    siblingCreated = true;
                    input.CopyTo(output);
                }
                if (Hash(sibling) != expected) throw new IOException("Update verification failed.");
                bool done = false;
                Exception last = null;
                for (int i = 0; i < 20 && !done; i++) {
                    try {
                        File.Replace(sibling, target, backup);
                        done = true;
                    } catch (Exception e) {
                        last = e;
                        Thread.Sleep(1000);
                    }
                }
                if (!done) throw last ?? new IOException("Update failed.");
                try { Process.Start(new ProcessStartInfo(target) { UseShellExecute = true }); }
                catch { File.Replace(backup, target, null); throw; }
                try { File.Delete(backup); } catch { }
            } finally {
                if (siblingCreated) try { if (File.Exists(sibling)) File.Delete(sibling); } catch { }
            }
        } catch {
            System.Windows.Forms.MessageBox.Show("Не удалось установить обновление. Запустите приложение и попробуйте снова.\n\nCouldn't install the update. Open the app and try again.", "R.E.P.O. Change Font");
        }
        return true;
    }
}
