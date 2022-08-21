using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using ManagedCode.Storage.Azure;
using Tenray.ZoneTree.AbstractFileStream;

namespace Tenray.ZoneTree.BlobFileSystem;

public class BlobFileStreamProvider : IFileStreamProvider
{
    private readonly string _connectionString;
    private readonly string _container;

    public BlobFileStreamProvider(string connectionString, string container)
    {
        _connectionString = connectionString;
        _container = container;
    }

    public IFileStream CreateFileStream(string path, FileMode mode, FileAccess access, FileShare share, 
        int bufferSize = BlobStream.DefaultBufferSize, FileOptions options = FileOptions.None)
    {
        if (bufferSize == 0)
        {
            return new BlobFileStream(CreateStream(NormalizePath(path)), access);
        }
        
        return new BlobFileStream(CreateBufferedStream(NormalizePath(path), bufferSize), access);
    }


    public void CreateDirectory(string path)
    {
        GetBlobContainerClient(path).CreateIfNotExists();
    }

    public bool DirectoryExists(string path)
    {
        return GetBlobContainerClient(path).Exists();
    }

    public void DeleteDirectory(string path, bool recursive)
    {
        GetBlobContainerClient(path).DeleteIfExists();
    }

    public bool FileExists(string path)
    {
        return GetPageBlobClient(path).Exists();
    }

    public void DeleteFile(string path)
    {
        GetPageBlobClient(path).DeleteIfExists();
    }

    public string ReadAllText(string path)
    {
        using var reader = new StreamReader(CreateBufferedStream(path));
        return reader.ReadToEnd();
    }

    public byte[] ReadAllBytes(string path)
    {
        using var reader = CreateBufferedStream(path);
        using MemoryStream ms = new MemoryStream();
        reader.CopyTo(ms);
        return ms.ToArray();
    }

    public void Replace(
        string sourceFileName,
        string destinationFileName,
        string destinationBackupFileName)
    {
        GetPageBlobClient(destinationFileName).CreateIfNotExists(0);
        var operation = GetPageBlobClient(destinationFileName).StartCopyFromUri(GetPageBlobClient(sourceFileName).Uri);
        operation.WaitForCompletionResponse();
    }

    public DurableFileWriter GetDurableFileWriter()
    {
        return new DurableFileWriter(this);
    }

    private string NormalizePath(string path)
    {
        return path.Replace('/', '\\').Replace($"{_container}\\", "");
    }

    internal Stream CreateBufferedStream(string fileName, int bufferSize = BlobStream.DefaultBufferSize)
    {
        fileName = NormalizePath(fileName);
        return new BufferedStream(new BlobStream(_connectionString, _container, fileName), bufferSize);
    }

    internal BlobStream CreateStream(string fileName)
    {
        fileName = NormalizePath(fileName);
        return new BlobStream(_connectionString, _container, fileName);
    }

    internal PageBlobClient GetPageBlobClient(string fileName)
    {
        BlobServiceClient blobServiceClient = new(_connectionString);

        var blobContainerClient =
            blobServiceClient.GetBlobContainerClient(_container);

        blobContainerClient.CreateIfNotExists();

        fileName = NormalizePath(fileName);
        var pageBlobClient = blobContainerClient.GetPageBlobClient(fileName);

        return pageBlobClient;
    }

    internal BlobContainerClient GetBlobContainerClient(string container)
    {
        BlobServiceClient blobServiceClient = new(_connectionString);
        var blobContainerClient = blobServiceClient.GetBlobContainerClient(_container);
        return blobContainerClient;
    }
}