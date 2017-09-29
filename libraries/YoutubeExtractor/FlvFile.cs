using System;
using System.IO;

namespace YoutubeExtractor
{
    internal class FlvFile : IDisposable
    {
        private readonly long _fileLength;
        private readonly string _outputPath;
        private IAudioExtractor _audioExtractor;
        private long _fileOffset;
        private FileStream _fileStream;

        /// <summary>
        /// Initializes a new instance of the <see cref="FlvFile"/> class.
        /// </summary>
        /// <param name="inputPath">The path of the input.</param>
        /// <param name="outputPath">The path of the output without extension.</param>
        public FlvFile(string inputPath, string outputPath)
        {
            _outputPath = outputPath;
            _fileStream = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024);
            _fileOffset = 0;
            _fileLength = _fileStream.Length;
        }

        public event EventHandler<ProgressEventArgs> ConversionProgressChanged;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <exception cref="AudioExtractionException">The input file is not an FLV file.</exception>
        public void ExtractStreams()
        {
            Seek(0);

            if (ReadUInt32() != 0x464C5601)
            {
                // not a FLV file
                throw new AudioExtractionException("Invalid input file. Impossible to extract audio track.");
            }

            ReadUInt8();
            var dataOffset = ReadUInt32();

            Seek(dataOffset);

            ReadUInt32();

            while (_fileOffset < _fileLength)
            {
                if (!ReadTag())
                {
                    break;
                }

                if (_fileLength - _fileOffset < 4)
                {
                    break;
                }

                ReadUInt32();

                var progress = _fileOffset * 1.0 / _fileLength * 100;

                ConversionProgressChanged?.Invoke(this, new ProgressEventArgs(progress));
            }

            CloseOutput(false);
        }

        private void CloseOutput(bool disposing)
        {
            if (_audioExtractor != null)
            {
                if (disposing && _audioExtractor.VideoPath != null)
                {
                    try
                    {
                        File.Delete(_audioExtractor.VideoPath);
                    }
                    catch
                    {
                        // ignored
                    }
                }

                _audioExtractor.Dispose();
                _audioExtractor = null;
            }
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_fileStream != null)
                {
                    _fileStream.Close();
                    _fileStream = null;
                }

                CloseOutput(true);
            }
        }

        private IAudioExtractor GetAudioWriter(uint mediaInfo)
        {
            var format = mediaInfo >> 4;

            if (format == 14 || format == 2)
            {
                return new Mp3AudioExtractor(_outputPath);
            }

            if (format == 10)
            {
                return new AacAudioExtractor(_outputPath);
            }

            string typeStr;

            switch (format)
            {
                case 1:
                    typeStr = "ADPCM";
                    break;

                case 6:
                case 5:
                case 4:
                    typeStr = "Nellymoser";
                    break;

                default:
                    typeStr = "format=" + format;
                    break;
            }

            throw new AudioExtractionException("Unable to extract audio (" + typeStr + " is unsupported).");
        }

        private byte[] ReadBytes(int length)
        {
            var buff = new byte[length];

            _fileStream.Read(buff, 0, length);
            _fileOffset += length;

            return buff;
        }

        private bool ReadTag()
        {
            if (_fileLength - _fileOffset < 11)
            {
                return false;
            }

            // Read tag header
            var tagType = ReadUInt8();
            var dataSize = ReadUInt24();
            var timeStamp = ReadUInt24();
            timeStamp |= ReadUInt8() << 24;
            ReadUInt24();

            // Read tag data
            if (dataSize == 0)
            {
                return true;
            }

            if (_fileLength - _fileOffset < dataSize)
            {
                return false;
            }

            var mediaInfo = ReadUInt8();
            dataSize -= 1;
            var data = ReadBytes((int)dataSize);

            if (tagType == 0x8)
            {
                // If we have no audio writer, create one
                if (_audioExtractor == null)
                {
                    _audioExtractor = GetAudioWriter(mediaInfo);
                }

                if (_audioExtractor == null)
                {
                    throw new InvalidOperationException("No supported audio writer found.");
                }

                _audioExtractor.WriteChunk(data, timeStamp);
            }

            return true;
        }

        private uint ReadUInt24()
        {
            var x = new byte[4];

            _fileStream.Read(x, 1, 3);
            _fileOffset += 3;

            return BigEndianBitConverter.ToUInt32(x, 0);
        }

        private uint ReadUInt32()
        {
            var x = new byte[4];

            _fileStream.Read(x, 0, 4);
            _fileOffset += 4;

            return BigEndianBitConverter.ToUInt32(x, 0);
        }

        private uint ReadUInt8()
        {
            _fileOffset += 1;
            return (uint)_fileStream.ReadByte();
        }

        private void Seek(long offset)
        {
            _fileStream.Seek(offset, SeekOrigin.Begin);
            _fileOffset = offset;
        }
    }
}
