using Microsoft.AspNetCore.Http;
using System.Text;
using System.Text.Json;

namespace ASC.Utilities
{
    public static class SessionExtensions
    {
        public static void SetObject<T>(this ISession session, string key, T value)
        {
            var jsonData = JsonSerializer.Serialize(value);
            var bytes = Encoding.UTF8.GetBytes(jsonData);
            session.Set(key, bytes);
        }

        public static T GetObject<T>(this ISession session, string key)
        {
            if (!session.TryGetValue(key, out byte[] value))
                return default;

            var jsonData = Encoding.UTF8.GetString(value);
            return JsonSerializer.Deserialize<T>(jsonData);
        }
        //public static void SetObject<T>(this ISession session, string key, T value)
        //{
        //    var jsonData = JsonSerializer.Serialize(value);
        //    var bytes = Encoding.UTF8.GetBytes(jsonData);
        //    session.Set(key, bytes);
        //}

        //public static T GetObject<T>(this ISession session, string key)
        //{
        //    if (!session.TryGetValue(key, out byte[] value))
        //        return default;

        //    var jsonData = Encoding.UTF8.GetString(value);
        //    return JsonSerializer.Deserialize<T>(jsonData);
        //}
    }
}