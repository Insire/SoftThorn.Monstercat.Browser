using Serilog;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SoftThorn.Monstercat.Browser.Core
{
    public class ReadFullyStream : Stream
    {
        private readonly Stream _sourceStream;
        private readonly ILogger _logger;
        private readonly byte[] _readAheadBuffer;

        private long pos; // psuedo-position
        private int readAheadLength;
        private int readAheadOffset;

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => pos;

        public override long Position
        {
            get
            {
                return pos;
            }
            set
            {
                throw new InvalidOperationException();
            }
        }

        public ReadFullyStream(Stream sourceStream, ILogger logger)
        {
            _sourceStream = sourceStream;
            _logger = logger.ForContext<ReadFullyStream>();
            _readAheadBuffer = new byte[4096];
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var bytesRead = 0;
            while (bytesRead < count)
            {
                var readAheadAvailableBytes = readAheadLength - readAheadOffset;
                var bytesRequired = count - bytesRead;
                if (readAheadAvailableBytes > 0)
                {
                    var toCopy = Math.Min(readAheadAvailableBytes, bytesRequired);
                    Array.Copy(_readAheadBuffer, readAheadOffset, buffer, offset + bytesRead, toCopy);
                    bytesRead += toCopy;
                    readAheadOffset += toCopy;
                }
                else
                {
                    readAheadOffset = 0;
                    readAheadLength = _sourceStream.Read(_readAheadBuffer, 0, _readAheadBuffer.Length);
                    _logger.Verbose("Read {ReadAheadLength} bytes (requested {Bytes})", readAheadLength, _readAheadBuffer.Length);
                    if (readAheadLength == 0)
                    {
                        break;
                    }
                }
            }
            pos += bytesRead;
            return bytesRead;
        }

        public override void Flush()
        {
            throw new InvalidOperationException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new InvalidOperationException();
        }

        public override void SetLength(long value)
        {
            throw new InvalidOperationException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            _sourceStream.Dispose();
        }
    }
}
