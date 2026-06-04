using Amazon.S3.Model;

namespace Marked.Features.Uploads;

public interface IS3Service
{
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType);

    Task<GetObjectResponse> GetObjectAsync(string bucketName, string key);
    
    Task DeleteAsync(string fileUrl);
}