using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace ForecastBuildTime.MongoDBModels
{
    public static class MongoDBExtensions
    {
        public static IAsyncEnumerable<T> Wrap<T>(this IAsyncCursor<T> cursor)
        {
            return new MongoDBEnumerable<T>(cursor);
        }

        public static async IAsyncEnumerable<T> Wrap<T>(this Task<IAsyncCursor<T>> cursorTask)
        {
            var cursor = await cursorTask.ConfigureAwait(false);
            await foreach (var t in cursor.Wrap())
                yield return t;
        }

        public static async IAsyncEnumerable<T> AsAsyncEnumerable<T>(this IAsyncCursorSource<T> cursorSource)
        {
            var cursor = await cursorSource.ToCursorAsync().ConfigureAwait(false);
            while (await cursor.MoveNextAsync().ConfigureAwait(false))
            {
                foreach (var doc in cursor.Current)
                {
                    yield return doc;
                }
            }
        }

        public static IAsyncEnumerable<T> AsAsyncEnumerable<T>(this IMongoQueryable<T> queryable)
        {
            return AsAsyncEnumerable(queryable as IAsyncCursorSource<T>);
        }
    }

    public class MongoDBEnumerable<T> : IAsyncEnumerable<T>, IAsyncEnumerator<T>
    {
        private readonly IAsyncCursor<T> _cursor;
        private IEnumerator<T>? _current;

        public MongoDBEnumerable(IAsyncCursor<T> cursor)
        {
            _cursor = cursor;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = new CancellationToken())
        {
            return this;
        }

        public ValueTask DisposeAsync()
        {
            _cursor.Dispose();
            return ValueTask.CompletedTask;
        }

        public async ValueTask<bool> MoveNextAsync()
        {
            if (_current?.MoveNext() != true)
            {
                if (!await _cursor.MoveNextAsync())
                    return false;
                _current = _cursor.Current.GetEnumerator();
                return _current.MoveNext();
            }

            return true;
        }

        public T Current
        {
            get
            {
                if (_current == null)
                {
                    throw new InvalidOperationException();
                }

                return _current.Current;
            }
        }
    }
}