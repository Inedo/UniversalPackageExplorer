using System;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace UniversalPackageExplorer
{
    public sealed class UniversalPackageFile : INotifyPropertyChanged
    {
        private ZipArchiveEntry entry;

        internal UniversalPackageFile(UniversalPackage.FileCollection collection, string fullName, ZipArchiveEntry entry)
        {
            this.Collection = collection;
            this.FullName = fullName;
            this.Length = entry.Length;
            this.entry = entry;
            this.children = new Lazy<UniversalPackage.FileCollection.SubTreeCollection>(() => fullName.EndsWith("/", StringComparison.OrdinalIgnoreCase) ? new UniversalPackage.FileCollection.SubTreeCollection(collection, fullName) : null);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public UniversalPackage.FileCollection Collection { get; }
        public string FullName { get; private set; }
        public string Name
        {
            get
            {
                if (!this.FullName.Contains("/"))
                {
                    return this.FullName;
                }

                int i = this.FullName.LastIndexOf('/', this.FullName.Length - 2);
                if (i == -1)
                {
                    return this.FullName.TrimEnd('/');
                }

                return this.FullName.Substring(i).Trim('/');
            }
        }
        public long Length { get; private set; }
        public DateTimeOffset LastWriteTime
        {
            get => this.entry.LastWriteTime;
            set
            {
                this.entry.LastWriteTime = value;
                this.Collection.package.Dirty = true;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LastWriteTime)));
            }
        }
        private readonly Lazy<UniversalPackage.FileCollection.SubTreeCollection> children;
        public UniversalPackage.FileCollection.SubTreeCollection Children => this.children.Value;

        public Task RenameAsync(string newName)
        {
            return this.Collection.RenameAsync(this, newName);
        }

        public Task DeleteAsync()
        {
            return this.Collection.DeleteAsync(this);
        }

        internal void DeleteInternal()
        {
            this.entry.Delete();
        }

        internal async Task RenameToAsync(string fullName, ZipArchiveEntry newEntry)
        {
            if (!this.FullName.EndsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                using (var input = this.entry.Open())
                using (var output = newEntry.Open())
                {
                    await input.CopyToAsync(output);
                }
            }

            newEntry.LastWriteTime = this.entry.LastWriteTime;

            this.entry.Delete();
            this.entry = newEntry;

            this.FullName = fullName;
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FullName)));
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
        }

        private void EnsureNotDirectory()
        {
            if (this.FullName.EndsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Cannot read or write to a directory.");
            }
        }

        private byte[] GetBytes()
        {
            this.EnsureNotDirectory();

            using (var memory = new MemoryStream())
            using (var stream = this.entry.Open())
            {
                stream.CopyTo(memory);
                return memory.ToArray();
            }
        }

        public Stream OpenRead()
        {
            return new MemoryStream(this.GetBytes(), false);
        }

        public Stream OpenWrite()
        {
            return new WriteStream(this, this.GetBytes());
        }

        public async Task CopyToAsync(Stream stream)
        {
            this.EnsureNotDirectory();

            using (var input = this.entry.Open())
            {
                await input.CopyToAsync(stream);
            }
        }

        private void CopyFrom(Stream stream)
        {
            this.EnsureNotDirectory();

            using (var output = this.entry.Open())
            {
                output.SetLength(0);
                stream.CopyTo(output);
                this.Length = output.Length;
            }

            this.LastWriteTime = DateTimeOffset.UtcNow;
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Length)));
        }

        public async Task CopyFromAsync(Stream stream)
        {
            this.EnsureNotDirectory();

            using (var output = this.entry.Open())
            {
                output.SetLength(0);
                await stream.CopyToAsync(output);
                this.Length = output.Length;
            }

            this.LastWriteTime = DateTimeOffset.UtcNow;
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Length)));
        }

        private sealed class WriteStream : Stream
        {
            private readonly MemoryStream inner = new MemoryStream();
            private readonly UniversalPackageFile file;

            public WriteStream(UniversalPackageFile file, byte[] contents)
            {
                this.file = file;
                this.Write(contents, 0, contents.Length);
                this.Position = 0;
            }

            public override bool CanRead => this.inner.CanRead;
            public override bool CanSeek => this.inner.CanSeek;
            public override bool CanWrite => this.inner.CanWrite;
            public override long Length => this.inner.Length;

            public override long Position
            {
                get => this.inner.Position;
                set => this.inner.Position = value;
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return this.inner.Read(buffer, offset, count);
            }
            public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
            {
                return this.inner.BeginRead(buffer, offset, count, callback, state);
            }
            public override int EndRead(IAsyncResult asyncResult)
            {
                return this.inner.EndRead(asyncResult);
            }
            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                return this.inner.ReadAsync(buffer, offset, count, cancellationToken);
            }
            public override int ReadByte()
            {
                return this.inner.ReadByte();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return this.inner.Seek(offset, origin);
            }

            public override void SetLength(long value)
            {
                this.inner.SetLength(value);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                this.inner.Write(buffer, offset, count);
            }
            public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
            {
                return this.inner.BeginWrite(buffer, offset, count, callback, state);
            }
            public override void EndWrite(IAsyncResult asyncResult)
            {
                this.inner.EndWrite(asyncResult);
            }
            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                return this.inner.WriteAsync(buffer, offset, count, cancellationToken);
            }
            public override void WriteByte(byte value)
            {
                this.inner.WriteByte(value);
            }

            public override void Flush()
            {
                var oldPosition = this.inner.Position;
                try
                {
                    this.inner.Position = 0;
                    this.file.CopyFrom(this.inner);
                }
                finally
                {
                    this.inner.Position = oldPosition;
                }
            }
            public override async Task FlushAsync(CancellationToken cancellationToken)
            {
                var oldPosition = this.inner.Position;
                try
                {
                    this.inner.Position = 0;
                    await this.file.CopyFromAsync(this.inner);
                }
                finally
                {
                    this.inner.Position = oldPosition;
                }
            }

            private bool disposed;
            protected override void Dispose(bool disposing)
            {
                if (!this.disposed)
                {
                    this.disposed = true;
                    this.Flush();
                    this.inner.Dispose();
                }
                base.Dispose(disposing);
            }
        }
    }
}