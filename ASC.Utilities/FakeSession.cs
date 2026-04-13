using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ASC.Tests
{
    public class FakeSession : ISession
    {
        private readonly Dictionary<string, byte[]> _storage = new Dictionary<string, byte[]>();

        public IEnumerable<string> Keys => _storage.Keys;

        public string Id => "FakeSessionId";

        public bool IsAvailable => true;

        public void Clear()
        {
            _storage.Clear();
        }

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task LoadAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public void Remove(string key)
        {
            if (_storage.ContainsKey(key))
            {
                _storage.Remove(key);
            }
        }

        public void Set(string key, byte[] value)
        {
            _storage[key] = value;
        }

        public bool TryGetValue(string key, out byte[] value)
        {
            return _storage.TryGetValue(key, out value);
        }
    }
}