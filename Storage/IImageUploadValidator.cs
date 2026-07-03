using Microsoft.AspNetCore.Http;

namespace BoardGameClubSoftware.Storage;

public interface IImageUploadValidator
{
    ImageUploadValidationResult Validate(IFormFile? file);
}
