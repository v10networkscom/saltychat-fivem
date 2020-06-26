using System;

namespace SaltyClient
{
    public static class Util
    {
        public static string ToJson(object obj)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(obj);
        }

        public static bool TryParseJson<T>(string json, out T result)
        {
            result = default;

            try
            {
                result = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
            }
            catch(Exception e)
            {
                throw new Exception($"Cannot parse json in {result?.GetType().Name}", e);
            }

            return result is object;
        }
    }
}
