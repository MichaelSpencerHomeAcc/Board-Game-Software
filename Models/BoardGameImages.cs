using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Board_Game_Software.Models
{
    public class BoardGameImages
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("SQLTable")]
        public string? SQLTable { get; set; }

        [BsonElement("GID")]
        [BsonRepresentation(BsonType.String)]
        public Guid? GID { get; set; }

        [BsonElement("ImageTypeGID")]
        [BsonRepresentation(BsonType.String)]
        public Guid? ImageTypeGID { get; set; }

        [BsonElement("Description")]
        public string? Description { get; set; }

        [BsonElement("ImageBytes")]
        public byte[]? ImageBytes { get; set; }

        [BsonElement("ContentType")]
        public string ContentType { get; set; } = "image/jpeg";

        [BsonElement("AvatarFocusX")]
        public int AvatarFocusX { get; set; } = 50;

        [BsonElement("AvatarFocusY")]
        public int AvatarFocusY { get; set; } = 50;

        [BsonElement("PodiumFocusX")]
        public int PodiumFocusX { get; set; } = 50;

        [BsonElement("PodiumFocusY")]
        public int PodiumFocusY { get; set; } = 50;

        [BsonElement("CardFocusX")]
        public int CardFocusX { get; set; } = 50;

        [BsonElement("CardFocusY")]
        public int CardFocusY { get; set; } = 50;

        [BsonElement("AvatarZoom")]
        public int AvatarZoom { get; set; } = 100;

        [BsonElement("PodiumZoom")]
        public int PodiumZoom { get; set; } = 100;
    }
}