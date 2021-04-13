using System.Runtime.Serialization;
using System.Xml.Linq;

namespace Jellyfin.Plugin.Listenbrainz.Models.Musicbrainz.Responses
{
    public class BaseResponse
    {
        [DataMember(Name = "error")]
        public string Error { get; set; }

        public XElement XmlData { get; set; }

        public BaseResponse(XElement xmlData)
        {
            XmlData = xmlData;
        }

        public BaseResponse(string error)
        {
            Error = error;
        }

        public virtual string GetData() => "";
    }
}
