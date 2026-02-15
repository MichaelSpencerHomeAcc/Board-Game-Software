using Board_Game_Software.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace Board_Game_Software.Controllers
{
    [Route("media")]
    public class MediaController : Controller
    {
        private readonly IMongoCollection<BoardGameImages> _images;
        private readonly Guid _boardGameFrontTypeGid;

        public MediaController(IMongoClient mongoClient, IConfiguration config)
        {
            var dbName = config["MongoDbSettings:Database"];
            _images = mongoClient.GetDatabase(dbName).GetCollection<BoardGameImages>("BoardGameImages");

            var frontTypeStr = config["Media:BoardGameFrontImageTypeGid"];
            if (!Guid.TryParse(frontTypeStr, out _boardGameFrontTypeGid))
            {
                // If missing, this endpoint will 404 until configured correctly
                _boardGameFrontTypeGid = Guid.Empty;
            }
        }

        [HttpGet("marker-type/{gid:guid}")]
        public async Task<IActionResult> MarkerType(Guid gid)
        {
            var filter = Builders<BoardGameImages>.Filter.And(
                Builders<BoardGameImages>.Filter.Eq(x => x.SQLTable, "bgd.BoardGameMarkerType"),
                Builders<BoardGameImages>.Filter.Eq(x => x.GID, gid)
            );

            var img = await _images.Find(filter).FirstOrDefaultAsync();
            if (img?.ImageBytes == null || img.ImageBytes.Length == 0)
                return NotFound();

            var contentType = string.IsNullOrWhiteSpace(img.ContentType) ? "application/octet-stream" : img.ContentType;
            Response.Headers["Cache-Control"] = "public,max-age=604800";
            return File(img.ImageBytes, contentType);
        }

        [HttpGet("player/{gid:guid}")]
        public async Task<IActionResult> Player(Guid gid)
        {
            var filter = Builders<BoardGameImages>.Filter.And(
                Builders<BoardGameImages>.Filter.Eq(x => x.SQLTable, "bgd.Player"),
                Builders<BoardGameImages>.Filter.Eq(x => x.GID, gid)
            );

            var img = await _images.Find(filter).FirstOrDefaultAsync();
            if (img?.ImageBytes == null || img.ImageBytes.Length == 0)
                return NotFound();

            var contentType = string.IsNullOrWhiteSpace(img.ContentType) ? "application/octet-stream" : img.ContentType;
            Response.Headers["Cache-Control"] = "public,max-age=604800";
            return File(img.ImageBytes, contentType);
        }

        [HttpGet("boardgame/front/{gid:guid}")]
        public async Task<IActionResult> BoardGameFront(Guid gid)
        {
            if (_boardGameFrontTypeGid == Guid.Empty)
                return NotFound("BoardGameFrontImageTypeGid not configured.");

            var filter = Builders<BoardGameImages>.Filter.And(
                Builders<BoardGameImages>.Filter.Eq("GID", gid.ToString()),
                Builders<BoardGameImages>.Filter.Eq("ImageTypeGID", _boardGameFrontTypeGid.ToString()),
                Builders<BoardGameImages>.Filter.In("SQLTable", new[] { "bgd.BoardGame", "BoardGames" })
            );

            var img = await _images.Find(filter).FirstOrDefaultAsync();
            if (img?.ImageBytes == null || img.ImageBytes.Length == 0)
                return NotFound();

            var contentType = DetectContentType(img.ImageBytes, img.ContentType);
            Response.Headers["Cache-Control"] = "public,max-age=604800";
            return File(img.ImageBytes, contentType);
        }

        private static string DetectContentType(byte[] bytes, string? fallback)
        {
            if (bytes.Length >= 12)
            {
                if (bytes[0] == (byte)'R' && bytes[1] == (byte)'I' && bytes[2] == (byte)'F' && bytes[3] == (byte)'F' &&
                    bytes[8] == (byte)'W' && bytes[9] == (byte)'E' && bytes[10] == (byte)'B' && bytes[11] == (byte)'P')
                    return "image/webp";
            }

            if (bytes.Length >= 3)
            {
                if (bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
                    return "image/jpeg";
            }

            if (bytes.Length >= 8)
            {
                if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47 &&
                    bytes[4] == 0x0D && bytes[5] == 0x0A && bytes[6] == 0x1A && bytes[7] == 0x0A)
                    return "image/png";
            }

            if (!string.IsNullOrWhiteSpace(fallback))
                return fallback;

            return "application/octet-stream";
        }

    }
}
