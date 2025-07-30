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
    }
}
