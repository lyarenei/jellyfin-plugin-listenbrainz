using System.Linq;
using System.Xml.Linq;

namespace Jellyfin.Plugin.Listenbrainz.Models.Musicbrainz.Responses
{
    public class RecordingIdResponse : BaseResponse
    {
        public RecordingIdResponse(XElement xmlData) : base(xmlData) { }

        public override string GetData()
        {
            try
            {
                return (string)XmlData.Descendants("recording").First().Attribute("id");
            }
            catch (System.Exception)
            {
                return base.GetData();
            }
        }
    }
}
