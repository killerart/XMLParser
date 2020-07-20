using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Xml;

namespace XMLParser {
    class Program {
        const string APP_PATH = "https://localhost:5001";
        static void Main(string[] args) {
            var apps = Directory.GetDirectories("xml-localizations");
            foreach (var app in apps) {
                var nameStart = app.LastIndexOf('/') + 1;
                if (nameStart != -1) {
                    var appName = app[nameStart..];
                    //var clientHandler = new HttpClientHandler {
                    //    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; }
                    //};
                    //using (var client = new HttpClient(clientHandler)) {
                    //    var response = client.PostAsync($"{APP_PATH}/api/app?name={appName}", new StringContent("")).Result;
                    //    Console.WriteLine($"Post {appName} creation: {response.StatusCode}");
                    //}
                    var languages = Directory.GetDirectories($"{app}");
                    var keys = new HashSet<string>();
                    foreach (var language in languages) {
                        var start = language.LastIndexOf("values");
                        if (start != -1) {
                            start += 7;
                            start = Math.Min(start, language.Length);
                            var name = language[start..];
                            if (string.IsNullOrWhiteSpace(name))
                                name = "en";
                            keys.UnionWith(ParseLanguage(name, language));
                        }
                    }
                    //clientHandler = new HttpClientHandler {
                    //    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; }
                    //};
                    //using (var client = new HttpClient(clientHandler)) {
                    //    var response = client.PutAsJsonAsync($"{APP_PATH}/api/app/{appName}", keys).Result;
                    //    Console.WriteLine($"Put {appName}: {response.StatusCode} {response.Content.ReadAsStringAsync().Result}");
                    //}
                }
            }
        }
        static HashSet<string> ParseLanguage(string langName, string directory) {
            var files = Directory.GetFiles(directory, "strings.xml");
            var keys = new HashSet<string>();
            HttpClientHandler clientHandler;
            foreach (var file in files) {
                var translations = ParseXMLFile(file);
                keys.UnionWith(translations.Keys);
                clientHandler = new HttpClientHandler {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; }
                };
                using(var client = new HttpClient(clientHandler)) {
                    var response = client.PostAsJsonAsync($"{APP_PATH}/api/locale", new { Name = langName, Translations = translations }).Result;
                    Console.WriteLine($"Post {langName} locale: {response.StatusCode} {response.Content.ReadAsStringAsync().Result}");
                }
            }
            return keys;
        }
        static Dictionary<string, string> ParseXMLFile(string path) {
            var translations = new Dictionary<string, string>();
            string key = string.Empty;
            string value = string.Empty;

            using(var reader = XmlReader.Create(path, new XmlReaderSettings { Async = true })) {
                while (reader.Read()) {
                    switch (reader.NodeType) {
                        case XmlNodeType.Element:
                            if (reader.Name == "string") {
                                key = reader.GetAttribute("name").Trim();
                            }
                            break;
                        case XmlNodeType.Text:
                            value = reader.GetValueAsync().Result;
                            break;
                        case XmlNodeType.EndElement:
                            if (reader.Name == "string") {
                                if (!translations.ContainsKey(key)) {
                                    translations.Add(key, value);
                                }
                            }
                            break;
                    }
                }
            }

            return translations;
        }
    }
}