using System;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace UniversalPackageExplorer.UPack
{
    public sealed class UniversalPackageFile : MarshalByRefObject, INotifyPropertyChanged
    {
        internal UniversalPackageFile(UniversalPackage.FileCollection collection, string fullName)
        {
            this.Collection = collection;
            this.FullName = fullName;
            this.children = new Lazy<UniversalPackage.FileCollection.SubTreeCollection>(() => fullName.EndsWith("/", StringComparison.OrdinalIgnoreCase) ? new UniversalPackage.FileCollection.SubTreeCollection(collection, fullName) : null);
        }

        private T EntryRead<T>(Func<ZipArchiveEntry, T> func)
        {
            using (var zip = this.Collection.package.OpenRead())
            {
                return func(zip.GetEntry(this.Collection.Prefix + this.FullName));
            }
        }
        private async Task<T> EntryReadAsync<T>(Func<ZipArchiveEntry, Task<T>> func)
        {
            using (var zip = this.Collection.package.OpenRead())
            {
                return await func(zip.GetEntry(this.Collection.Prefix + this.FullName));
            }
        }
        private async Task EntryReadAsync(Func<ZipArchiveEntry, Task> func)
        {
            using (var zip = this.Collection.package.OpenRead())
            {
                await func(zip.GetEntry(this.Collection.Prefix + this.FullName));
            }
        }
        private void EntryWrite(Action<ZipArchiveEntry> func)
        {
            using (var zip = this.Collection.package.OpenWrite())
            {
                func(zip.GetEntry(this.Collection.Prefix + this.FullName) ?? zip.CreateEntry(this.Collection.Prefix + this.FullName, this.Children == null ? CompressionLevel.Optimal : CompressionLevel.NoCompression));
            }
        }
        private async Task EntryWriteAsync(Func<ZipArchiveEntry, Task> func)
        {
            using (var zip = this.Collection.package.OpenWrite())
            {
                await func(zip.GetEntry(this.Collection.Prefix + this.FullName) ?? zip.CreateEntry(this.Collection.Prefix + this.FullName, this.Children == null ? CompressionLevel.Optimal : CompressionLevel.NoCompression));
            }
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
        public long Length => this.EntryRead(e => e.Length);
        public DateTimeOffset LastWriteTime
        {
            get => this.EntryRead(e => e.LastWriteTime);

            set
            {
                this.EntryWrite(e => e.LastWriteTime = value);
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
            using (var zip = this.Collection.package.OpenWrite())
            {
                zip.GetEntry(this.Collection.Prefix + this.FullName)?.Delete();
            }
        }

        internal async Task RenameToAsync(string fullName, ZipArchiveEntry oldEntry, ZipArchiveEntry newEntry)
        {
            if (!this.FullName.EndsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                using (var input = oldEntry.Open())
                using (var output = newEntry.Open())
                {
                    await input.CopyToAsync(output);
                }
            }

            newEntry.LastWriteTime = oldEntry.LastWriteTime;
            oldEntry.Delete();

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

            return this.EntryRead(e =>
            {
                using (var memory = new MemoryStream())
                using (var stream = e.Open())
                {
                    stream.CopyTo(memory);
                    return memory.ToArray();
                }
            });
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

            await this.EntryReadAsync(async e =>
            {
                using (var input = e.Open())
                {
                    await input.CopyToAsync(stream);
                }
            });
        }

        private void CopyFrom(Stream stream)
        {
            this.EnsureNotDirectory();

            this.EntryWrite(e =>
            {
                using (var output = e.Open())
                {
                    output.SetLength(0);
                    stream.CopyTo(output);
                }
            });

            this.LastWriteTime = DateTimeOffset.UtcNow;
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Length)));
        }

        public async Task CopyFromAsync(Stream stream)
        {
            this.EnsureNotDirectory();

            await this.EntryWriteAsync(async e =>
            {
                using (var output = e.Open())
                {
                    output.SetLength(0);
                    await stream.CopyToAsync(output);
                }
            });

            this.LastWriteTime = DateTimeOffset.UtcNow;
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Length)));
        }

        public async Task ImportFromFileSystemAsync(string folderName)
        {
            foreach (var dir in Directory.EnumerateDirectories(folderName))
            {
                var subdir = await this.Collection.CreateDirectoryAsync(this.FullName + Path.GetFileName(dir) + '/');
                await subdir.ImportFromFileSystemAsync(dir);
            }

            foreach (var fileName in Directory.EnumerateFiles(folderName))
            {
                var file = await this.Collection.CreateFileAsync(this.FullName + Path.GetFileName(fileName));
                using (var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan))
                {
                    await file.CopyFromAsync(stream);
                }
            }
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