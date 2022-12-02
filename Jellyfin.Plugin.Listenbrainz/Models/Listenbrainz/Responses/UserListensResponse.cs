namespace Jellyfin.Plugin.Listenbrainz.Models.Listenbrainz.Responses
{
    /// <summary>
    /// Response model for user listens.
    /// </summary>
    public class UserListensResponse : BaseResponse
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserListensResponse"/> class.
        /// </summary>
        public UserListensResponse()
        {
            Payload = new UserListensPayload();
        }

        /// <summary>
        /// Gets or sets response payload.
        /// </summary>
        public UserListensPayload Payload { get; set; }

        /// <inheritdoc />
        public override bool IsError() => Error != null;
    }
}
