// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon.S3.Model;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
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

    private List<byte> _syncTempBuffer;
    private SemaphoreSlim _semaphore;

    Task _writeSynchronousArtifacts;
    bool _finalize;

    public MultiPartUploadStream(IS3Storage client, string bucketName, string objectKey, string uploadId, int bufferSize)
    {
        _bufferSize = Math.Max(bufferSize, MinimumSize); // has to be at least the minimum
        _buffer = ArrayPool<byte>.Shared.Rent(_bufferSize);
        _syncTempBuffer = new();
        _offset = 0;
        _client = client;
        _bucketName = bucketName;
        _objectKey = objectKey;
        _uploadId = uploadId;
        _semaphore = new SemaphoreSlim(1, 1);
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
        _finalize = true;

        if (_writeSynchronousArtifacts != null)
            await _writeSynchronousArtifacts;
        
        if (Closed)
            throw new ObjectDisposedException(nameof(MultiPartUploadStream));
        if (_offset == 0)
            return;

        await DoWriteAsync(true, cancellationToken);
    }

    public async Task StartAsyncLoop(CancellationToken cancellationToken)
    {
        while (!_finalize || _syncTempBuffer.Count > 0)
        {
            if (Closed)
                throw new ObjectDisposedException(nameof(MultiPartUploadStream));

            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                await WriteAsync(_syncTempBuffer.ToArray(), _finalize, cancellationToken);
                _syncTempBuffer.Clear();
            }
            finally
            {
                _semaphore.Release();
            }

            await Task.Delay(100, cancellationToken); // Delay is arbitrary
        }
    }

    public override void Flush()
    {
        throw new NotSupportedException();
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
        await WriteAsync(buffer, false, cancellationToken);
    }

    public async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, bool allowPartialWrite, CancellationToken cancellationToken)
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
            if (BytesAvailableInBuffer() == 0 || allowPartialWrite)
                await DoWriteAsync(allowPartialWrite, cancellationToken);
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

        await using var stream = new MemoryStream(_buffer, 0, _offset);
        stream.Position = 0;
        // use _parts.Count + 1 to avoid a part #0 (part numbers must not be less than 1)
        var eTag = await _client.UploadPartAsync(_uploadId, _parts.Count + 1, _offset, stream, cancellationToken);
        _parts.Add(eTag);
        _offset = 0;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        if (Closed)
            throw new ObjectDisposedException(nameof(MultiPartUploadStream));

        _semaphore.Wait();
        try
        {
            _syncTempBuffer.AddRange(buffer.AsMemory().Slice(offset, count).ToArray().AsEnumerable());
        }
        finally
        {
            _semaphore.Release();
        }
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
