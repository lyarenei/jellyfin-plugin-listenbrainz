using System.Web;

namespace Jellyfin.Plugin.ListenBrainz.Http;

/// <summary>
/// HTTP utils.
/// </summary>
public static class Utils
{
    /// <summary>
    /// Convert dictionary to HTTP GET query.
    /// </summary>
    /// <param name="requestData">Query data.</param>
    /// <returns>Query string.</returns>
    public static string ToHttpGetQuery(Dictionary<string, string> requestData)
    {
        var query = string.Empty;
        int i = 0;
        foreach (var d in requestData)
        {
            var encodedKey = HttpUtility.UrlEncode(d.Key);
            var encodedValue = HttpUtility.UrlEncode(d.Value);
            query += $"{encodedKey}={encodedValue}";
            if (++i != requestData.Count)
            {
                query += '&';
            }
        }

        return query;
    }
}
