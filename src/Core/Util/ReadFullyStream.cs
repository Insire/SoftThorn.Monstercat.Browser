using System;
using System.Diagnostics;
using System.IO;

namespace SoftThorn.Monstercat.Browser.Core
{
    public class ReadFullyStream : Stream
    {
        private readonly Stream sourceStream;
        private readonly byte[] readAheadBuffer;

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

        public ReadFullyStream(Stream sourceStream)
        {
            this.sourceStream = sourceStream;
            readAheadBuffer = new byte[4096];
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
                    Array.Copy(readAheadBuffer, readAheadOffset, buffer, offset + bytesRead, toCopy);
                    bytesRead += toCopy;
                    readAheadOffset += toCopy;
                }
                else
                {
                    readAheadOffset = 0;
                    readAheadLength = sourceStream.Read(readAheadBuffer, 0, readAheadBuffer.Length);
                    Debug.WriteLine(string.Format("Read {0} bytes (requested {1})", readAheadLength, readAheadBuffer.Length));
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
    }
}
