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

    public IFileStream CreateFileStream(
        string path,
        FileMode mode,
        FileAccess access,
        FileShare share,
        int bufferSize = 4096,
        FileOptions options = FileOptions.None)
    {
        return new BlobFileStream(CreateBufferedStream(NormalizePath(path)));
        //return new BlobFileStream(CreateStream(NormalizePath(path)));
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
        throw new NotImplementedException();
    }

    public byte[] ReadAllBytes(string path)
    {
        throw new NotImplementedException();
    }

    public void Replace(
        string sourceFileName,
        string destinationFileName,
        string destinationBackupFileName)
    {
        // File Replace is a fast operation in local filesystem. 
        // It uses file rename operation and it is atomic.
        //File.Replace(sourceFileName, destinationFileName, destinationBackupFileName);
    }

    public DurableFileWriter GetDurableFileWriter()
    {
        return new DurableFileWriter(this);
    }

    private string NormalizePath(string path)
    {
        return path.Replace('/', '\\').Replace($"{_container}\\", "");
    }

    internal Stream CreateBufferedStream(string fileName)
    {
        fileName = NormalizePath(fileName);
        return new BufferedStream(new BlobStream(_connectionString, _container, fileName), BlobStream.DefaultBufferSize);
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