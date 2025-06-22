
// Enable this to display AppleDouble header values for debugging
//#define DEBUG_APPLE_DOUBLE

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace P4SweepCore
{
    class StreamReaderTimer
    {
        readonly System.Diagnostics.Stopwatch Timer = new System.Diagnostics.Stopwatch();

        // Total time spent waiting for stream, in seconds
        public double TotalWaitSeconds => (((double)Timer.ElapsedMilliseconds) / 1000.0);

        public string ReadLine(StreamReader Reader)
        {
            Timer.Start();

            var Result = Reader.ReadLine();

            Timer.Stop();

            return Result;
        }
    }

    class UTF16ToUTF8TranscodeStream : Stream
    {
        StreamReader SourceReader;
        Encoder TextEncoder = Encoding.UTF8.GetEncoder();

        char[] TextBuffer = new char[P4Sweep.IOBufferSize];

        // We allocate 2x memory because UTF-16 chars are not likely to be more than 2 bytes each
        byte[] SourceBuffer = new byte[P4Sweep.IOBufferSize * 2];
        int SourceBufferIndex = 0;
        int SourceBufferSize = 0;

        int GetSourceByte()
        {
            if (SourceBufferIndex >= SourceBufferSize)
            {
                // Fetch more data from the source
                int CharsRead = SourceReader.Read(TextBuffer);

                // Convert UTF-16 chars to UTF-8 chars. Allocate more memory if needed.
                ArgumentException BufferTooSmallException = null;
                do
                {
                    BufferTooSmallException = null;

                    try
                    {
                        SourceBufferSize = TextEncoder.GetBytes(TextBuffer, 0, CharsRead, SourceBuffer, 0, false);
                    }
                    catch (ArgumentException Ex)
                    {
                        // Increase the size of the encoder output buffer and try again
                        SourceBuffer = new byte[SourceBuffer.Length * 2];
                        BufferTooSmallException = Ex;
                    }

                } while (BufferTooSmallException != null);

                SourceBufferIndex = 0;

                if (SourceBufferSize == 0)
                {
                    // EOF!
                    return (-1);
                }
            }

            int Result = SourceBuffer[SourceBufferIndex];
            SourceBufferIndex++;

            return Result;
        }

        public UTF16ToUTF8TranscodeStream(Stream InSourceStream)
        {
            SourceReader = new StreamReader(InSourceStream, Encoding.Unicode, true, P4Sweep.IOBufferSize);
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotImplementedException();

        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int BytesFilled = 0;

            // Try to read all requested bytes
            while (BytesFilled < count)
            {
                // Get a byte from the source stream
                int SourceByte = GetSourceByte();

                if (SourceByte < 0)
                {
                    // EOF!
                    break;
                }

                // Write byte into read buffer
                buffer[offset + BytesFilled] = (byte)SourceByte;
                BytesFilled++;
            }

            return BytesFilled;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
    }

    class P4TranscodeTextStream : Stream
    {
        Stream SourceStream;

        byte[] SourceBuffer = new byte[P4Sweep.IOBufferSize];
        int SourceBufferIndex = 0;
        int SourceBufferSize = 0;

        public P4TranscodeTextStream(Stream InSourceStream, bool SkipByteOrderMark)
        {
            SourceStream = InSourceStream;

            // Skip BOM if requested
            if (SkipByteOrderMark)
            {
                //SourceStream.Position += 3;
                for (int i = 0; i < 3; i++)
                {
                    GetSourceByte();
                }
            }
        }

        int GetSourceByte()
        {
            if (SourceBufferIndex >= SourceBufferSize)
            {
                // Fetch more data from the source
                SourceBufferSize = SourceStream.Read(SourceBuffer);
                SourceBufferIndex = 0;

                if (SourceBufferSize == 0)
                {
                    // EOF!
                    return (-1);
                }
            }

            int Result = SourceBuffer[SourceBufferIndex];
            SourceBufferIndex++;

            return Result;
        }

        int PeekSourceByte()
        {
            int Result = GetSourceByte();

            if ((Result >= 0) && (SourceBufferIndex > 0))
            {
                // Seek back one byte
                SourceBufferIndex--;
            }

            return Result;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => SourceStream.Length;

        public override long Position { get => SourceStream.Position; set => SourceStream.Position = value; }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int BytesFilled = 0;

            // Try to read all requested bytes
            while (BytesFilled < count)
            {
                // Get a byte from the source stream
                int SourceByte = GetSourceByte();

                if (SourceByte < 0)
                {
                    // EOF!
                    break;
                }

                // TODO: Handle UTF-8 multi-byte chars correctly!!!
                if (SourceByte == '\r')
                {
                    // Peek the next byte
                    int NextSourceByte = PeekSourceByte();

                    if (NextSourceByte != '\n')
                    {
                        // Found CR without LF, so write CR
                        buffer[offset + BytesFilled] = Convert.ToByte('\r');
                        BytesFilled++;
                    }
                }
                else
                {
                    // Write byte into read buffer
                    buffer[offset + BytesFilled] = (byte)SourceByte;
                    BytesFilled++;
                }
            }

            return BytesFilled;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
    }

    class Utilities
    {
        // Escape special characters ('%', '*', '#', '@') to ASCII codes
        public static string P4EscapeFilename(string Filename)
        {
            return Filename.Replace("%", "%25").Replace("*", "%2A").Replace("#", "%23").Replace("@", "%40");
        }

        // Get the filename of the AppleDouble resource fork for the given data file
        public static string P4GetAppleDoubleResourceFilename(string AppleDoubleDataFilename)
        {
            return Path.Combine(Path.GetDirectoryName(AppleDoubleDataFilename), ("%" + Path.GetFileName(AppleDoubleDataFilename)));
        }
    }

    // Adapted from: https://datatracker.ietf.org/doc/html/rfc1740
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct AppleSingleDoubleHeader /* header portion of AppleSingle / AppleDouble */
    {
        /* AppleSingle = 0x00051600; AppleDouble = 0x00051607 */
        public const uint AppleSingleMagic = 0x00051600;
        public const uint AppleDoubleMagic = 0x00051607;

        public uint magicNum;      /* internal file type tag */
        public uint versionNum;    /* format version: 2 = 0x00020000 */

        // char filler[16];        /* filler, currently all bits 0 */
        public uint Filler1;
        public uint Filler2;
        public uint Filler3;
        public uint Filler4;

        public ushort numEntries;  /* number of entries which follow */
    };

    // Adapted from: https://datatracker.ietf.org/doc/html/rfc1740
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct AppleSingleDoubleEntry /* one AppleSingle / AppleDouble entry descriptor */
    {
        public const uint DataForkID = 1;

        public uint entryID;     /* entry type: see list, 0 invalid */
        public uint entryOffset; /* offset, in octets, from beginning of file to this entry's data */
        public uint entryLength; /* length of data in octets */
    };

    public static class AppleSingleDoubleUtilities
    {
        public class ParseException : Exception
        {
            public ParseException()
            {
            }

            public ParseException(string message)
                : base(message)
            {
            }

            public ParseException(string message, Exception inner)
                : base(message, inner)
            {
            }
        }

        // Converts an AppleDouble file to an AppleSingle file
        public static byte[] AppleDoubleToAppleSingle(byte[] ResourceFork, byte[] DataFork)
        {
            // Get the header
            AppleSingleDoubleHeader Header = FromBytesBigEndian32Bit<AppleSingleDoubleHeader>(ResourceFork);

            // Verify
            if (Header.magicNum != AppleSingleDoubleHeader.AppleDoubleMagic)
            {
                throw new ParseException($"Unable to parse AppleDouble header! Expected 0x{AppleSingleDoubleHeader.AppleDoubleMagic:X8} but got 0x{Header.magicNum:X8}.");
            }

            // Read the entries
            int AppleDoubleEntryBytes = ((int)Header.numEntries * Marshal.SizeOf<AppleSingleDoubleEntry>());
            var Entries = ReadEntries(Header.numEntries, ResourceFork.Skip(Marshal.SizeOf<AppleSingleDoubleHeader>()).Take(AppleDoubleEntryBytes).ToArray());

#if DEBUG_APPLE_DOUBLE
            Console.WriteLine("Original AppleDouble header:");
            Console.Write(Header.LogValues());
            foreach(var Entry in Entries)
            {
                Console.Write(Entry.LogValues());
            }
#endif

            // Switch the header to an AppleSingle header
            Header.magicNum = AppleSingleDoubleHeader.AppleSingleMagic;

            // We're going to add one entry for the data fork
            Header.numEntries++;

            // Update the existing entry offsets to account for the new entry we're adding
            for (int i = 0; i < Entries.Length; i++)
            {
                Entries[i].entryOffset += (uint)Marshal.SizeOf<AppleSingleDoubleEntry>();
            }

            // Create the data fork entry
            Entries = Entries.Append(new AppleSingleDoubleEntry()).ToArray();
            Entries[Header.numEntries - 1].entryID = AppleSingleDoubleEntry.DataForkID;
            Entries[Header.numEntries - 1].entryOffset = (uint)(ResourceFork.Length + Marshal.SizeOf<AppleSingleDoubleEntry>());
            Entries[Header.numEntries - 1].entryLength = (uint)(DataFork.Length);

            byte[] HeaderBytes = ToBytesBigEndian32Bit(Header);
            byte[] EntriesBytes = WriteEntries(Entries);
            byte[] HeaderlessResourceForkBytes = ResourceFork.Skip(HeaderBytes.Length + AppleDoubleEntryBytes).ToArray();

            // Create the AppleSingle file
            byte[] Result = HeaderBytes.Concat(EntriesBytes).Concat(HeaderlessResourceForkBytes).Concat(DataFork).ToArray();

            // Verify the size
            int ExpectedSize = (ResourceFork.Length + DataFork.Length + Marshal.SizeOf<AppleSingleDoubleEntry>());
            if (Result.Length != ExpectedSize)
            {
                throw new ApplicationException($"Unable to create AppleSingle file! Expected {ExpectedSize} bytes but got {Result.Length} bytes.");
            }

#if DEBUG_APPLE_DOUBLE
            Console.WriteLine("New AppleSingle header:");
            Console.Write(Header.LogValues());
            foreach (var Entry in Entries)
            {
                Console.Write(Entry.LogValues());
            }
#endif

            return Result;
        }

        static AppleSingleDoubleEntry[] ReadEntries(uint NumEntries, byte[] EntryData)
        {
            int BytesPerEntry = Marshal.SizeOf<AppleSingleDoubleEntry>();

            var Result = new AppleSingleDoubleEntry[NumEntries];

            for (int i = 0; i < NumEntries; i++)
            {
                Result[i] = FromBytesBigEndian32Bit<AppleSingleDoubleEntry>(EntryData.Skip(i * BytesPerEntry).Take(BytesPerEntry).ToArray());

                if (Result[i].entryID == 0)
                {
                    throw new ParseException($"Unable to parse AppleDouble entry! Invalid entry ID: {Result[i].entryID}");
                }
            }

            return Result;
        }

        static byte[] WriteEntries(AppleSingleDoubleEntry[] Entries)
        {
            byte[] Result = new byte[0];

            foreach (var Entry in Entries)
            {
                Result = Result.Concat(ToBytesBigEndian32Bit(Entry)).ToArray();
            }

            return Result;
        }

        // Byte-swaps all DWORDs for big-endian, then byte-swaps any remaining WORD.
        static byte[] ByteSwapBigEndian32And16Bit(byte[] Input)
        {
            // Sanity check
            if ((Input.Length % 2) != 0)
            {
                throw new ApplicationException($"Unable to perform 32/16-bit byte swap on {Input.Length} bytes!");
            }

            byte[] Result = new byte[Input.Length];

            if (BitConverter.IsLittleEndian)
            {
                // Swap dwords
                int NumDWords = (Input.Length / 4);
                for (int i = 0; i < NumDWords; i++)
                {
                    int Index = (i * 4);
                    Result[Index + 0] = Input[Index + 3];
                    Result[Index + 1] = Input[Index + 2];
                    Result[Index + 2] = Input[Index + 1];
                    Result[Index + 3] = Input[Index + 0];
                }

                // Swap words
                int NumWords = ((Input.Length - (NumDWords * 4)) / 2);
                for (int i = 0; i < NumWords; i++)
                {
                    int Index = ((NumDWords * 4) + (i * 2));
                    Result[Index + 0] = Input[Index + 1];
                    Result[Index + 1] = Input[Index + 0];
                }
            }
            else
            {
				Input.CopyTo(Result, 0);
            }

            return Result;
        }

        static byte[] ToBytesBigEndian32Bit<T>(T Header)
        {
            int Size = Marshal.SizeOf(Header);
            byte[] Result = new byte[Size];

            IntPtr StructData = Marshal.AllocHGlobal(Size);
            Marshal.StructureToPtr(Header, StructData, false);
            Marshal.Copy(StructData, Result, 0, Size);
            Marshal.FreeHGlobal(StructData);

            return ByteSwapBigEndian32And16Bit(Result);
        }

        static T FromBytesBigEndian32Bit<T>(byte[] HeaderBytes)
            where T : struct
        {
            T Result = new T();
            int Size = Marshal.SizeOf(Result);

            IntPtr StructData = Marshal.AllocHGlobal(Size);
            Marshal.Copy(ByteSwapBigEndian32And16Bit(HeaderBytes.Take(Size).ToArray()), 0, StructData, Size);
            Result = Marshal.PtrToStructure<T>(StructData);
            Marshal.FreeHGlobal(StructData);

            return Result;
        }

#if DEBUG_APPLE_DOUBLE
        static string LogValues(this AppleSingleDoubleHeader Header)
        {
            var Builder = new StringBuilder();

            Builder.AppendLine("Header:");

            Builder.AppendLine($"Magic:   0x{Header.magicNum:X8}");
            Builder.AppendLine($"Version: 0x{Header.versionNum:X8}");

            Builder.AppendLine($"Filler1: 0x{Header.Filler1:X8}");
            Builder.AppendLine($"Filler2: 0x{Header.Filler2:X8}");
            Builder.AppendLine($"Filler3: 0x{Header.Filler3:X8}");
            Builder.AppendLine($"Filler4: 0x{Header.Filler4:X8}");

            Builder.AppendLine($"Entries: {Header.numEntries}");

            return Builder.ToString();
        }

        static string LogValues(this AppleSingleDoubleEntry Entry)
        {
            var Builder = new StringBuilder();

            Builder.AppendLine("Entry:");

            Builder.AppendLine($"ID: {Entry.entryID}");
            Builder.AppendLine($"Offset: {Entry.entryOffset}");
            Builder.AppendLine($"Length: {Entry.entryLength}");

            return Builder.ToString();
        }
#endif
    }
}
