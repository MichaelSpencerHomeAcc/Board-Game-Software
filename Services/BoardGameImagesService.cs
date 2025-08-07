using Board_Game_Software.Models;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Board_Game_Software.Settings;

namespace Board_Game_Software.Services
{
    public class BoardGameImagesService
    {
        private readonly IMongoCollection<BoardGameImages> _collection;

        public BoardGameImagesService(IConfiguration configuration, IMongoClient client)
        {
            var settings = configuration.GetSection("MongoDbSettings").Get<MongoDbSettings>();
            if (settings == null)
            {
                throw new InvalidOperationException("MongoDbSettings section is missing or invalid.");
            }
            var database = client.GetDatabase(settings.Database);
            _collection = database.GetCollection<BoardGameImages>("BoardGameImages");
        }

        public async Task<List<BoardGameImages>> GetAllAsync() =>
            await _collection.Find(_ => true).ToListAsync();

        public async Task<BoardGameImages> GetByIdAsync(string id) =>
            await _collection.Find(img => img.Id == id).FirstOrDefaultAsync();

        public async Task UploadAsync(BoardGameImages image) =>
            await _collection.InsertOneAsync(image);

        public async Task DeleteAsync(string id) =>
            await _collection.DeleteOneAsync(img => img.Id == id);

        public async Task<Dictionary<Guid, string?>> GetFrontImagesAsync(
    IEnumerable<Guid> boardGameGids,
    Guid frontTypeGid)
        {
            var gids = boardGameGids.Distinct().Where(g => g != Guid.Empty).ToArray();
            if (gids.Length == 0) return new();

            var filter = Builders<BoardGameImages>.Filter.And(
                Builders<BoardGameImages>.Filter.In(x => x.GID, gids.Select(g => (Guid?)g)),
                Builders<BoardGameImages>.Filter.Eq(x => x.ImageTypeGID, (Guid?)frontTypeGid)
            );


            var docs = await _collection.Find(filter).ToListAsync();

            return gids.ToDictionary(
                g => g,
                g =>
                {
                    var doc = docs.FirstOrDefault(d => d.GID == g);
                    return (doc?.ImageBytes != null && !string.IsNullOrWhiteSpace(doc.ContentType))
                        ? $"data:{doc.ContentType};base64,{Convert.ToBase64String(doc.ImageBytes)}"
                        : null;
                });
        }

            public async Task<string?> GetFrontImageAsync(Guid boardGameGid, Guid frontTypeGid)
        {
            var filter = Builders<BoardGameImages>.Filter.And(
                Builders<BoardGameImages>.Filter.Eq(x => x.GID, boardGameGid),
                Builders<BoardGameImages>.Filter.Eq(x => x.ImageTypeGID, frontTypeGid)
            );

            var doc = await _collection.Find(filter).FirstOrDefaultAsync();
            return (doc?.ImageBytes != null && !string.IsNullOrWhiteSpace(doc.ContentType))
                ? $"data:{doc.ContentType};base64,{Convert.ToBase64String(doc.ImageBytes)}"
                : null;
        }

    }

}

