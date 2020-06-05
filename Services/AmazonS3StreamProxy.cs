using System.IO;
using Amazon.S3.IO;

namespace Codesanook.AmazonS3.Services
{
    public class AmazonS3StreamProxy : Stream
    {
        private readonly Stream stream;
        private readonly IAmazonS3StorageProvider provider;
        private readonly S3FileInfo fileInfo;

        public AmazonS3StreamProxy(Stream stream, IAmazonS3StorageProvider provider, S3FileInfo fileInfo)
        {
            this.stream = stream;
            this.provider = provider;
            this.fileInfo = fileInfo;
        }

        public override bool CanRead => stream.CanRead;
        public override bool CanSeek => stream.CanSeek;
        public override bool CanWrite => stream.CanWrite;
        public override void Flush() => stream.Flush();
        public override long Length => stream.Length;

        public override long Position {
            get => stream.Position;
            set => stream.Position = value;
        }

        public override int Read(byte[] buffer, int offset, int count)
            => stream.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin)
            => stream.Seek(offset, origin);

        public override void SetLength(long value) => stream.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count)
            => stream.Write(buffer, offset, count);

        protected override void Dispose(bool disposing)
        {
            stream.Dispose();
            provider.PublishFile(fileInfo.FullName);
        }
    }
}
