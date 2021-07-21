//#define GOOGLE_AUTH

#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

#if GOOGLE_AUTH
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
#endif

namespace GameTools
{
    public class GoogleSheetConfig : List<GoogleSheet>
    {
    }

    public class AtlassianSheetConfig : Dictionary<object, string>
    {
    }

    public class GoogleSheet
    {
        public object name;
        public string id;
        public Dictionary<object, string> pages = new Dictionary<object, string>();
    }

    public class SpreadsheetLoader
    {
        public SpreadsheetLoader(string pathToConfigs, GoogleSheetConfig googleConfig, AtlassianSheetConfig atlassianConfig)
        {
            this.pathToConfigs = pathToConfigs;
            this.googleConfig = googleConfig;
            this.atlassianConfig = atlassianConfig;
        }

        string pathToConfigs;
        GoogleSheetConfig googleConfig;
        AtlassianSheetConfig atlassianConfig;

        private static List<List<string>> SkipStartLines(List<List<string>> table, int skipStartLines)
            => table.GetRange(skipStartLines, table.Count - skipStartLines);

        private static List<List<string>> GetXmlTable(string page, int skipStartLines = 1)
        {
            page = Regex.Replace(page, @"<\/?ac[a-zA-Z\-\:\=\ \""0-9]*>", string.Empty);
            var x = XDocument.Parse(page);
            var table = new List<List<string>>();

            foreach (var rowElement in x.Root.Elements())
            {
                table.Add(rowElement.Elements().Select(cellelement =>
                {
                    StringBuilder sb = new StringBuilder();
                    ParseThisXMLShitRecursively(sb, cellelement);
                    return sb.ToString();
                }).ToList());
            }

            return SkipStartLines(table, skipStartLines);

            void ParseThisXMLShitRecursively(StringBuilder sb, XElement elem)
            {
                var paragraphCount = 0;
                foreach (var node in elem.Nodes())
                {
                    if (node.NodeType == XmlNodeType.Text)
                    {
                        sb.Append(((XText)node).Value);
                    }
                    else if (node.NodeType == XmlNodeType.Element)
                    {
                        var subElem = (XElement)node;
                        if (subElem.Name == "br" && elem.Name != "p" && elem.Name != "span")
                        {
                            sb.Append('\n');
                        }
                        else
                        {
                            if (subElem.Name == "p") paragraphCount++;
                            if (paragraphCount > 1) sb.Append("\n");
                            ParseThisXMLShitRecursively(sb, subElem);
                        }
                    }
                }
            }
        }

        public CsvReader Page(object page)
        {
            var docName = googleConfig.Find(c => c.pages.ContainsKey(page)).name.ToString();
            var di = new DirectoryInfo(pathToConfigs);
            di = di.GetDirectories().First(info => info.Name.StartsWith(docName));
            var source = File.ReadAllText(Path.Combine(di.FullName, page + ".csv"));
            return new CsvReader(source.Split(new[] { "\r\n" }, StringSplitOptions.None));
        }

        public void DownloadAllConfigs()
        {
            Authorize();
            Connect();

            DirectoryInfo directoryInfo =
                Directory.CreateDirectory(pathToConfigs);

            foreach (var file in directoryInfo.GetFiles())
                file.Delete();
            foreach (var dir in directoryInfo.GetDirectories())
                dir.Delete(true);

            var count = googleConfig.Sum(sheet => sheet.pages.Count) + atlassianConfig.Count;
            var part = 1f / count;
            var i = 0;

            var filesToWrite = new List<(string, string)>();

            foreach (var config in googleConfig)
                foreach (var page in config.pages)
                {
                    EditorUtility.DisplayProgressBar($"Downloading {config.name} page", $"Page {page.Key}", i++ * part);
                    var content = LoadTableAsCSV(config.id, page.Value);
                    var path = $"{pathToConfigs}{config.name}";
                    Directory.CreateDirectory(path);
                    filesToWrite.Add(($"{path}/{page.Key}.csv", content));
                }

            foreach (var config in atlassianConfig)
            {
                var content = DownloadAtlassianTable(config.Value, config.Key.ToString(), part, i++);
                filesToWrite.Add(($"{pathToConfigs}{config.Key}.csv", content));
            }

            foreach (var (path, content) in filesToWrite)
            {
                File.WriteAllText(path, content);
            }

            EditorUtility.ClearProgressBar();
        }

        private string LoadTableAsCSV(string tableId, string pageId)
        {
            ServicePointManager.ServerCertificateValidationCallback = MyRemoteCertificateValidationCallback;

            var link = $"https://docs.google.com/spreadsheets/d/{tableId}/export?format=csv&id={tableId}&gid={pageId}";

            string csv = null;
            var client = new WebClient();
            try
            {
                csv = new StreamReader(client.OpenRead(link)).ReadToEnd();
            }
            catch (Exception e)
            {
                Debug.LogError($"cant download config {link}, error = {e.Message}, stack = {e.StackTrace}");
            }

            return csv;
        }

        private static string DownloadAtlassianTable(string id, string name, float part = 1, int i = 0)
        {
            const string url =
                "https://omnigames.atlassian.net/wiki/plugins/viewstorage/viewpagestorage.action?pageId=";
            var www = UnityWebRequest.Get($"{url}{id}");
            www.SendWebRequest();
            while (!www.isDone)
                EditorUtility.DisplayProgressBar($"Downloading atlassian configs", $"Page {name}",
                    www.downloadProgress / part + part * i);

            if (part == 1 || i == 0)
                EditorUtility.ClearProgressBar();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error while downloading config {name} id={id} from atlassian.\n {www.error}");
                return null;
            }

            var tBody = "<tbody>";

            var text = www.downloadHandler.text;
            text = text.Substring(text.IndexOf(tBody), text.Length - text.IndexOf(tBody));
            text = text.Substring(0, text.IndexOf("</tbody>") + "</tbody>".Length);
            text = text.Replace("&nbsp;", " ");
            return text;
        }

        private bool HasCredentialOnDisk => Directory.Exists(GetCredentialsPath());

        private string GetCredentialsPath()
        {
            return Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                ".credentials\\sheets.googleapis.com-RifftersAR.json"));
        }

#if GOOGLE_AUTH
    private UserCredential credential = null;
    private DriveService service = null;
#endif

        private void Authorize()
        {
#if GOOGLE_AUTH
        var stream =
 new FileStream("Assets/OmniLibs/ConfigAndLocalizationTools/Editor/client_secret.json", FileMode.Open,
            FileAccess.Read);

        try
        {
            credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.Load(stream).Secrets,
                new string[] {DriveService.Scope.Drive},
                "user",
                CancellationToken.None,
                new FileDataStore(GetCredentialsPath(), true)).Result;
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            throw;
        }
        finally
        {
            stream.Dispose();
        }
#endif
        }

        private void Connect()
        {
#if GOOGLE_AUTH
        if (credential == null) return;

        service = new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "Config Uploader",
        });
#endif
        }

        private static bool MyRemoteCertificateValidationCallback(object sender, X509Certificate certificate,
            X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            var isOk = true;
            // If there are errors in the certificate chain,
            // look at each error to determine the cause.
            if (sslPolicyErrors != SslPolicyErrors.None)
            {
                for (int i = 0; i < chain.ChainStatus.Length; i++)
                {
                    if (chain.ChainStatus[i].Status == X509ChainStatusFlags.RevocationStatusUnknown)
                        continue;

                    chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
                    chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                    chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(0, 1, 0);
                    chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllFlags;
                    if (chain.Build((X509Certificate2)certificate))
                        continue;
                    isOk = false;
                    break;
                }
            }

            return isOk;
        }
    }
}
#endif