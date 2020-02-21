using System.IO;
using Amazon.S3.IO;

namespace Codesanook.AmazonS3.Services
{
    public class AmazonS3StreamProxy : Stream
    {
        private readonly Stream _stream;
        private readonly IAmazonS3StorageProvider _provider;
        private readonly S3FileInfo _fileInfo;

        public AmazonS3StreamProxy(Stream stream, IAmazonS3StorageProvider provider, S3FileInfo fileInfo)
        {
            _stream = stream;
            _provider = provider;
            _fileInfo = fileInfo;
        }

        public override bool CanRead => _stream.CanRead;
        public override bool CanSeek => _stream.CanSeek;
        public override bool CanWrite => _stream.CanWrite;
        public override void Flush() => _stream.Flush();
        public override long Length => _stream.Length;

        public override long Position {
            get => _stream.Position;
            set => _stream.Position = value;
        }

        public override int Read(byte[] buffer, int offset, int count)
            => _stream.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin)
            => _stream.Seek(offset, origin);

        public override void SetLength(long value) => _stream.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count)
            => _stream.Write(buffer, offset, count);

        protected override void Dispose(bool disposing)
        {
            _stream.Dispose();
            _provider.PublishFile(_fileInfo.FullName);
        }
    }
}
