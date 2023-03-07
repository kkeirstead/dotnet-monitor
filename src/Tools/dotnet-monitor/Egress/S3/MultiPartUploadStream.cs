// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon.S3.Model;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress.S3;

internal class MultiPartUploadStream : Stream
{
    private readonly byte[] _buffer;
    private int _offset;
    private readonly string _bucketName;
    private readonly string _objectKey;
    private readonly string _uploadId;
    private readonly IS3Storage _client;
    private readonly List<PartETag> _parts = new();
    public List<PartETag> Parts => _parts.ToList();
    public bool Closed { get; private set; }
    private int _position;
    public const int MinimumSize = 5 * 1024 * 1024; // the minimum size of an upload part (except for the last part)
    private readonly int _bufferSize;

    private Pipe pipe = new Pipe();

    Task _writeSynchronousArtifacts;
    Stream _pipeWStream;
    Stream _pipeRStream;


    int byteCount;  // this is not safe, testing only
    bool end;

    SemaphoreSlim semaphore = new(1, 1);

    public MultiPartUploadStream(IS3Storage client, string bucketName, string objectKey, string uploadId, int bufferSize)
    {
        _bufferSize = Math.Max(bufferSize, MinimumSize); // has to be at least the minimum
        _buffer = ArrayPool<byte>.Shared.Rent(_bufferSize);
        _client = client;
        _bucketName = bucketName;
        _objectKey = objectKey;
        _uploadId = uploadId;
        _pipeWStream = pipe.Writer.AsStream();
        _pipeRStream = pipe.Reader.AsStream();
    }

    public MultiPartUploadStream(IS3Storage client, string bucketName, string objectKey, string uploadId, int bufferSize, CancellationToken token) : this(client, bucketName, objectKey, uploadId, bufferSize)
    {
        _writeSynchronousArtifacts = StartAsyncLoop(token);
    }

    public override async Task FlushAsync(CancellationToken cancellationToken)
    {
        if (Closed)
            throw new ObjectDisposedException(nameof(MultiPartUploadStream));
        await DoWriteAsync(false, cancellationToken);
    }

    public async Task FinalizeAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("StartFinalize");

        await pipe.Writer.CompleteAsync();

        _pipeWStream.Dispose();

        Console.WriteLine("Between S and M Finalize");

        end = true;

        await _writeSynchronousArtifacts;

        Console.WriteLine("MiddleFinalize");

        if (Closed)
            throw new ObjectDisposedException(nameof(MultiPartUploadStream));
        if (_offset == 0)
            return;

        await DoWriteAsync(true, cancellationToken);

        Console.WriteLine("EndFinalize");
    }

    public async Task StartAsyncLoop(CancellationToken cancellationToken)
    {
        Console.WriteLine("StartAsyncLoop");

        var previousPipePosition = 0;

        while (!end)
        {
            if (Closed)
                throw new ObjectDisposedException(nameof(MultiPartUploadStream));

            await semaphore.WaitAsync(cancellationToken);

            if (previousPipePosition != byteCount)
            {
                Console.WriteLine("Pipe position: " + byteCount);

                byteCount -= previousPipePosition;

                byte[] buffer = new byte[byteCount];

                _pipeRStream.Read(buffer, 0, byteCount);

                await WriteAsync(buffer.ToArray(), cancellationToken);

                previousPipePosition = byteCount;
            }

            semaphore.Release();

            await Task.Delay(500, cancellationToken); // arbitrary
        }
    }

    public override void Flush()
    {
        //Console.WriteLine("Flush");
        //throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
    {
        if (Closed)
            throw new ObjectDisposedException(nameof(MultiPartUploadStream));

        int BytesAvailableInBuffer() { return _bufferSize - _offset; }
        int count = buffer.Length;
        int offset = 0;
        do
        {
            int bytesToCopy = Math.Min(count, BytesAvailableInBuffer());
            buffer.Slice(offset, bytesToCopy).CopyTo(_buffer.AsMemory(_offset));
            _offset += bytesToCopy; // move the offset of the stream buffer
            offset += bytesToCopy; // move offset of part buffer
            count -= bytesToCopy; // reduce amount of bytes which still needs to be written
            _position += bytesToCopy; // move global position

            // part buffer is full -> trigger upload of part
            if (BytesAvailableInBuffer() == 0)
                await DoWriteAsync(false, cancellationToken);
        } while (count > 0);
    }

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        await WriteAsync(buffer.AsMemory().Slice(offset, count), cancellationToken);
    }

    private async Task DoWriteAsync(bool allowPartialWrite, CancellationToken cancellationToken)
    {
        if (_offset == 0) // no data
            return;

        if (_offset < MinimumSize && !allowPartialWrite) // buffer not full
            return;

        Console.WriteLine("DoWriteAsync");

        await using var stream = new MemoryStream(_buffer, 0, _offset);
        stream.Position = 0;
        // use _parts.Count + 1 to avoid a part #0 (part numbers for AWS must not be less than 1)
        var eTag = await _client.UploadPartAsync(_uploadId, _parts.Count + 1, _offset, stream, cancellationToken);
        _parts.Add(eTag);
        _offset = 0;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        if (Closed)
            throw new ObjectDisposedException(nameof(MultiPartUploadStream));

        _pipeWStream.Write(buffer, offset, count);

        semaphore.Wait();

        byteCount += count;

        semaphore.Release();
    }

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => !Closed;
    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => _position;
        set => throw new NotSupportedException();
    }

    public override void Close()
    {
        if (Closed)
            return;
        Closed = true;
        ArrayPool<byte>.Shared.Return(_buffer);
        base.Close();
    }
}
