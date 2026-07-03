using Board_Game_Software.Models;
using Board_Game_Software.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;

namespace Board_Game_Software.Controllers
{
    [Route("media")]
    public class MediaController : Controller
    {
        private static readonly string[] ImageExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif", ".svg"];

        private readonly IMongoCollection<BoardGameImages> _images;
        private readonly Guid _boardGameFrontTypeGid;
        private readonly IBlobMediaStore _blobMediaStore;
        private readonly BoardGameDbContext _db;

        public MediaController(
            IMongoClient mongoClient,
            IConfiguration config,
            IBlobMediaStore blobMediaStore,
            BoardGameDbContext db)
        {
            _blobMediaStore = blobMediaStore;
            _db = db;

            var dbName = config["MongoDbSettings:Database"];
            _images = mongoClient.GetDatabase(dbName).GetCollection<BoardGameImages>("BoardGameImages");

            var frontTypeStr = config["Media:BoardGameFrontImageTypeGid"];
            if (!Guid.TryParse(frontTypeStr, out _boardGameFrontTypeGid))
            {
                _boardGameFrontTypeGid = Guid.Empty;
            }
        }

        [HttpGet("marker-type/{gid:guid}")]
        public async Task<IActionResult> MarkerType(Guid gid)
        {
            var blob = await FindBlobImageAsync($"marker-type/{gid:D}");
            if (blob != null) return BlobFile(blob);

            var filter = Builders<BoardGameImages>.Filter.And(
                Builders<BoardGameImages>.Filter.Eq(x => x.SQLTable, "bgd.BoardGameMarkerType"),
                Builders<BoardGameImages>.Filter.Eq(x => x.GID, gid)
            );

            var img = await _images.Find(filter).FirstOrDefaultAsync();
            return MongoFileOrNotFound(img);
        }

        [HttpGet("player/{gid:guid}")]
        public async Task<IActionResult> Player(Guid gid)
        {
            var blob = await FindBlobImageAsync($"player/{gid:D}");
            if (blob != null) return BlobFile(blob);

            var filter = Builders<BoardGameImages>.Filter.And(
                Builders<BoardGameImages>.Filter.Eq(x => x.SQLTable, "bgd.Player"),
                Builders<BoardGameImages>.Filter.Eq(x => x.GID, gid)
            );

            var img = await _images.Find(filter).FirstOrDefaultAsync();
            return MongoFileOrNotFound(img);
        }

        [HttpGet("publisher/{gid:guid}")]
        public async Task<IActionResult> Publisher(Guid gid)
        {
            var blob = await FindBlobImageAsync($"publisher/{gid:D}");
            if (blob != null) return BlobFile(blob);

            var filterGuid = Builders<BoardGameImages>.Filter.And(
                Builders<BoardGameImages>.Filter.In(x => x.SQLTable, new[] { "bgd.Publisher", "Publishers" }),
                Builders<BoardGameImages>.Filter.Eq(x => x.GID, (Guid?)gid)
            );

            var img = await _images.Find(filterGuid).FirstOrDefaultAsync();

            if (img?.ImageBytes == null || img.ImageBytes.Length == 0)
            {
                var filterString = Builders<BoardGameImages>.Filter.And(
                    Builders<BoardGameImages>.Filter.In("SQLTable", new[] { "bgd.Publisher", "Publishers" }),
                    Builders<BoardGameImages>.Filter.Eq("GID", gid.ToString())
                );

                img = await _images.Find(filterString).FirstOrDefaultAsync();
            }

            return MongoFileOrNotFound(img);
        }

        [HttpGet("boardgame/front/{gid:guid}")]
        public async Task<IActionResult> BoardGameFront(Guid gid)
        {
            var blobKeys = new List<string> { $"boardgame/front/{gid:D}" };
            var templateGid = await _db.BoardGames
                .AsNoTracking()
                .Where(g => g.Gid == gid && g.FkBgdTemplateBoardGameNavigation != null)
                .Select(g => (Guid?)g.FkBgdTemplateBoardGameNavigation!.Gid)
                .FirstOrDefaultAsync();

            if (templateGid.HasValue)
            {
                blobKeys.Add($"boardgame/front/{templateGid.Value:D}");
            }

            var blob = await FindBlobImageAsync(blobKeys.ToArray());
            if (blob != null) return BlobFile(blob);

            if (_boardGameFrontTypeGid == Guid.Empty)
                return NotFound("BoardGameFrontImageTypeGid not configured.");

            var filter = Builders<BoardGameImages>.Filter.And(
                Builders<BoardGameImages>.Filter.Eq("GID", gid.ToString()),
                Builders<BoardGameImages>.Filter.Eq("ImageTypeGID", _boardGameFrontTypeGid.ToString()),
                Builders<BoardGameImages>.Filter.In("SQLTable", new[] { "bgd.BoardGame", "BoardGames" })
            );

            var img = await _images.Find(filter).FirstOrDefaultAsync();
            return MongoFileOrNotFound(img);
        }

        private async Task<BlobMediaFile?> FindBlobImageAsync(params string[] baseKeys)
        {
            var keys = baseKeys
                .SelectMany(key => ImageExtensions.Select(extension => $"{key}{extension}"))
                .ToArray();

            return await _blobMediaStore.FindAsync(keys);
        }

        private IActionResult BlobFile(BlobMediaFile blob)
        {
            Response.Headers["Cache-Control"] = "public,max-age=604800";
            Response.Headers["X-Media-Source"] = "blob";
            return File(blob.Bytes, blob.ContentType);
        }

        private IActionResult MongoFileOrNotFound(BoardGameImages? img)
        {
            if (img?.ImageBytes == null || img.ImageBytes.Length == 0)
                return NotFound();

            var contentType = DetectContentType(img.ImageBytes, img.ContentType);
            Response.Headers["Cache-Control"] = "public,max-age=604800";
            Response.Headers["X-Media-Source"] = "mongo";
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
