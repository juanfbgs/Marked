using Amazon.S3;
using Amazon.S3.Model;

namespace Marked.Features.Uploads;

public class S3Service : IS3Service
{
    private readonly IAmazonS3 _s3;
    private readonly string _bucketName;

    public S3Service(IConfiguration config)
    {
        var awsOptions = config.GetSection("S3");
        _bucketName = awsOptions["BucketName"]!;

        var configS3 = new AmazonS3Config
        {
            ServiceURL = awsOptions["ServiceUrl"],
            ForcePathStyle = true,
            UseHttp = true,
        };

        _s3 = new AmazonS3Client(
            awsOptions["AccessKey"],
            awsOptions["SecretKey"],
            configS3
        );
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType)
    {
        var key = $"bookmarks/{Guid.NewGuid()}-{fileName}";

        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = fileStream,
            ContentType = contentType,
        };

        await _s3.PutObjectAsync(request);

        return key;
    }

    public async Task<string> GeneratePresignedUrlAsync(string bucketName, string key, int expiresInMinutes)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = bucketName,
            Key = key,
            Protocol = Protocol.HTTP,
            Expires = DateTime.UtcNow.AddMinutes(expiresInMinutes)
        };
        return await Task.FromResult(_s3.GetPreSignedURL(request));
    }

    public async Task<GetObjectResponse> GetObjectAsync(string bucketName, string key)
    {
        return await _s3.GetObjectAsync(bucketName, key);
    }

    public async Task DeleteAsync(string key)
    {
        await _s3.DeleteObjectAsync(_bucketName, key);
    }
}