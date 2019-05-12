using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mochi
{
    public class Router
    {
        private Dictionary<HTTPMethod, Dictionary<string, Func<Context, Task>>> handleFuncDict = new Dictionary<HTTPMethod, Dictionary<string, Func<Context, Task>>>();

        public void Register(HTTPMethod method, string path, Func<Context, Task> handleFunc)
        {
            Dictionary<string, Func<Context, Task>> dict;
            if (!handleFuncDict.TryGetValue(method, out dict))
            {
                dict = new Dictionary<string, Func<Context, Task>>(StringComparer.OrdinalIgnoreCase);
                handleFuncDict[method] = dict;
            }

            path = NormalizePath(path);

            if (dict.ContainsKey(path))
            {
                throw new InvalidOperationException($"Duplicate HandlerFunc. : {path}");
            }

            dict[path] = handleFunc;
        }

        public Func<Context, Task> Find(HTTPMethod method, string path)
        {
            Dictionary<string, Func<Context, Task>> dict;
            if (!handleFuncDict.TryGetValue(method, out dict)) return null;

            Func<Context, Task> handleFunc;
            path = NormalizePath(path);
            if (!dict.TryGetValue(path, out handleFunc)) return null;

            return handleFunc;
        }

        private string NormalizePath(string path)
        {
            if (path.Length == 0)
            {
                path = "/";
            }
            else
            {
                // Add first Slash
                if (path[0] != '/')
                {
                    path = "/" + path;
                }

                // Trim last slash
                if (path[path.Length - 1] == '/')
                {
                    path = path.Substring(0, path.Length - 1);
                }
            }

            return path;
        }
    }
}