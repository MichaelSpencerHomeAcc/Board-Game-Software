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
    }
}
