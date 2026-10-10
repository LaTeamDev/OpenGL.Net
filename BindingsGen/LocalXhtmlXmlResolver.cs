using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Xml;

namespace BindingsGen;

public class LocalXhtmlXmlResolver : XmlUrlResolver {
    private static string _dtdPath;
    private static HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(20) };
    private static Dictionary<Uri, Exception> _failedEntities = new();
    private static Lock _lock = new();

    public LocalXhtmlXmlResolver(string dtdPath, string documentPath) {
        DocumentPath = Path.GetDirectoryName(documentPath)??"";
    }

    public LocalXhtmlXmlResolver(string documentPath) : this(Path.Combine(Program.BasePath, "RefPages", "DTD"),
        documentPath) {
        
    }

    public readonly string DocumentPath;

    /// <summary>
    /// Rewrite known-dead hosts/paths to their current location, and prefer HTTPS.
    /// </summary>
    private static Uri NormalizeUri(Uri uri) {
        var builder = new UriBuilder(uri);
        
        if (builder.Scheme == Uri.UriSchemeHttp) {
            builder.Scheme = Uri.UriSchemeHttps;
            builder.Port = -1; // default port
        }

        return builder.Uri;
    }
    private static readonly Dictionary<string, string> _KnownUris = new()
    {
        ["-//OASIS//DTD DocBook MathML Module V1.1b1//EN"]    = "https://docbook.org/xml/mathml/1.1CR1/dbmathml.dtd",
        ["-//OASIS//DTD DocBook XML V4.3//EN"]                = "https://docbook.org/xml/4.3/docbookx.dtd",
        ["-//OASIS//ENTITIES DocBook Notations V4.3//EN"]     = "https://docbook.org/xml/4.3/dbnotnx.mod",
        ["-//OASIS//ENTITIES DocBook Character Entities V4.3//EN"] = "https://docbook.org/xml/4.3/dbcentx.mod",
        ["-//W3C//DTD XHTML 1.0 Transitional//EN"]            = "https://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd",
    };

    public override Uri ResolveUri(Uri baseUri, string relativeUri) {

        if (relativeUri != null && _KnownUris.TryGetValue(relativeUri, out string url))
            return new Uri(url);

        return base.ResolveUri(baseUri, relativeUri);
    }

    public override object? GetEntity(Uri absoluteUri, string? role, Type? ofObjectToReturn) {
        ArgumentNullException.ThrowIfNull(absoluteUri);

        if (ofObjectToReturn != typeof(Stream))
            return base.GetEntity(absoluteUri, role, ofObjectToReturn);

        if (absoluteUri.Scheme == Uri.UriSchemeHttp || absoluteUri.Scheme == Uri.UriSchemeHttps) {
            var fileName = Path.GetFileName(absoluteUri.AbsolutePath);
            var localPath = Path.Combine(_dtdPath, fileName);

            lock (_lock) {
                if (_failedEntities.ContainsKey(absoluteUri))
                    return null;

                if (!File.Exists(localPath)) {
                    var downloadUri = NormalizeUri(absoluteUri);
                    Console.Write("Downloading {0}...", downloadUri);

                    try {
                        Download(downloadUri, localPath);
                        Console.WriteLine(" done!");
                    } catch (Exception e) {
                        Console.WriteLine(" failed ({0})", e.Message);
                        _failedEntities.Add(absoluteUri, e);
                        return null;
                    }
                }
            }

            return new FileStream(localPath, FileMode.Open, FileAccess.Read);
        }

        if (absoluteUri.Scheme == Uri.UriSchemeFile) {
            var localPath = Path.Combine(DocumentPath, Path.GetFileName(absoluteUri.LocalPath));
            if (File.Exists(localPath))
                return new FileStream(localPath, FileMode.Open, FileAccess.Read);
        }

        return base.GetEntity(absoluteUri, role, ofObjectToReturn);
    }

    private static void Download(Uri uri, string localPath) {
        var exceptions = new List<Exception>();
        
        for (var tries = 0; tries < 3; tries++) {
            try {
                using (var request = new HttpRequestMessage(HttpMethod.Get, uri))
                using (var response = _http.Send(request)) {   // .NET 5+; use SendAsync(...).GetAwaiter().GetResult() on older targets
                    response.EnsureSuccessStatusCode();

                    // Write to a temp file so a partial download is never mistaken for a cached DTD
                    var tmp = localPath + ".tmp";
                    using (var src = response.Content.ReadAsStream())
                    using (var dst = File.Create(tmp))
                        src.CopyTo(dst);

                    File.Move(tmp, localPath, overwrite: true);
                    return;
                }
            } catch (Exception e) {
                exceptions.Add(new Exception("Failed to download "+uri+" (try "+tries+")", e));
                Thread.Sleep(500 * (tries + 1));
            }
        }

        throw new AggregateException(exceptions);
    }
}