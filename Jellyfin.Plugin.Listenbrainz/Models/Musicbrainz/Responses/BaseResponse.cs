using System.Xml.Linq;

namespace Jellyfin.Plugin.Listenbrainz.Models.Musicbrainz.Responses
{
    public class BaseResponse
    {
        public XElement XmlData { get; set; }

        public BaseResponse(XElement xmlData)
        {
            XmlData = xmlData;
        }

        public virtual string GetData() => "";
    }
}
