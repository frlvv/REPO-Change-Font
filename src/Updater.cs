using System;
using System.Net;
using System.Collections.Generic;
using System.Web.Script.Serialization;

static class AppUpdater {
    const string Api = "https://api.github.com/repos/frlvv/REPO-Change-Font/releases/latest";
    const string ReleaseRoot = "https://github.com/frlvv/REPO-Change-Font/releases/tag/";
    public sealed class Release { public string Version, PageUrl; }

    sealed class TimedClient : WebClient {
        protected override WebRequest GetWebRequest(Uri address) {
            var request = base.GetWebRequest(address);
            request.Timeout = 15000;
            var http = request as HttpWebRequest;
            if (http != null) http.ReadWriteTimeout = 15000;
            return request;
        }
    }
    static Release NewerRelease(string tag) {
        if (!System.Text.RegularExpressions.Regex.IsMatch(tag ?? "", @"^[vV]?\d+\.\d+\.\d+(\.\d+)?$")) return null;
        Version latest;
        if (!Version.TryParse(tag.TrimStart('v', 'V'), out latest) || latest <= System.Reflection.Assembly.GetExecutingAssembly().GetName().Version) return null;
        return new Release { Version = tag, PageUrl = ReleaseRoot + Uri.EscapeDataString(tag) };
    }
    public static Release Check() {
        ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
        try {
            using (var client = new TimedClient()) {
                client.Headers[HttpRequestHeader.UserAgent] = "REPO-Change-Font-Updater";
                var data = new JavaScriptSerializer().DeserializeObject(client.DownloadString(Api)) as Dictionary<string, object>;
                if (data == null || (data.ContainsKey("draft") && Convert.ToBoolean(data["draft"])) ||
                    (data.ContainsKey("prerelease") && Convert.ToBoolean(data["prerelease"]))) return null;
                return NewerRelease(Convert.ToString(data["tag_name"]));
            }
        } catch (WebException) {
            var request = (HttpWebRequest)WebRequest.Create("https://github.com/frlvv/REPO-Change-Font/releases/latest");
            request.UserAgent = "REPO-Change-Font-Updater";
            request.Timeout = 15000;
            request.ReadWriteTimeout = 15000;
            using (var response = (HttpWebResponse)request.GetResponse()) {
                var uri = response.ResponseUri;
                const string prefix = "/frlvv/REPO-Change-Font/releases/tag/";
                if (uri.Scheme != "https" || !uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) ||
                    !uri.AbsolutePath.StartsWith(prefix, StringComparison.Ordinal)) return null;
                return NewerRelease(Uri.UnescapeDataString(uri.AbsolutePath.Substring(prefix.Length)));
            }
        }
    }
}
