using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mochi
{
    public class Router
    {
        private Dictionary<HTTPMethod, Dictionary<string, Func<Context, Task>>> staticHandleFuncDict = new Dictionary<HTTPMethod, Dictionary<string, Func<Context, Task>>>();
        private Dictionary<HTTPMethod, BoxedRouteEntry> anyRootDict = new Dictionary<HTTPMethod, BoxedRouteEntry>();

        public void Register(HTTPMethod method, string path, Func<Context, Task> handleFunc)
        {
            path = NormalizePath(path);

            if (IsStaticPath(path))
            {
                RegisterStatic(method, path, handleFunc);
            }
            else
            {
                RegisterAny(method, path, handleFunc);
            }
        }

        private void RegisterStatic(HTTPMethod method, string path, Func<Context, Task> handleFunc)
        {
            Dictionary<string, Func<Context, Task>> dict;
            if (!staticHandleFuncDict.TryGetValue(method, out dict))
            {
                dict = new Dictionary<string, Func<Context, Task>>(StringComparer.OrdinalIgnoreCase);
                staticHandleFuncDict[method] = dict;
            }


            if (dict.ContainsKey(path))
            {
                throw new InvalidOperationException($"Duplicate HandlerFunc. : {path}");
            }

            dict[path] = handleFunc;
        }

        private void RegisterAny(HTTPMethod method, string path, Func<Context, Task> handleFunc)
        {
            var anyIndex = path.IndexOf('*');
            if (anyIndex < 0)
            {
                throw new ArgumentException($"'${path}' is not any path");
            }

            BoxedRouteEntry boxedAnyRoot;
            if (!this.anyRootDict.TryGetValue(method, out boxedAnyRoot))
            {
                this.anyRootDict[method] = new BoxedRouteEntry()
                {
                    Entry = new RouteEntry(path.Substring(0, anyIndex), 0, null, handleFunc),
                };
                return;
            }

            ref var entry = ref boxedAnyRoot.Entry;
            var index = 1;
            var remain = anyIndex - 1;
            while (true)
            {
                var startIndex = index - 1;
                var entryPath = entry.Path;
                if (entryPath.Length - 1 == remain)
                {
                    if (entry.HandleFunc != null)
                    {
                        throw new InvalidOperationException($"Duplicate HandlerFunc. : {path}");
                    }

                    entry = new RouteEntry(entry.Path, entry.ChildCount, entry.Children, handleFunc);
                    return;
                }

                for (var i = 1; i < entryPath.Length && index < anyIndex; i++, index++)
                {
                    if (entryPath[i] != path[index])
                    {
                        var newPath = entryPath.Substring(0, i);
                        var children = new RouteEntry[10];
                        children[0] = new RouteEntry(entryPath.Substring(i, entryPath.Length - i), entry.ChildCount, entry.Children, entry.HandleFunc);
                        children[1] = new RouteEntry(path.Substring(index, anyIndex - index), 0, null, handleFunc);
                        entry = new RouteEntry(newPath, 2, children, null);
                        return;
                    }
                }

                remain -= (entryPath.Length - 1);

                // if entryPath is longer than path
                // ex) 
                //     Path is /abc
                //
                //     Entry is
                //     entry:
                //       path: /abcde
                //       children:
                //         - entry:
                //             path: fg
                //         - entry:
                //             path: jk
                //
                //     Merge Path to entry
                //     entry:
                //       path: /abc
                //       children:
                //         - entry:
                //             path: de
                //             children:
                //               - entry:
                //                   path: fg
                //               - entry:
                //                   path: jk
                if (remain <= 0)
                {
                    var children = new RouteEntry[10];
                    children[0] = new RouteEntry(entry.Path.Substring(anyIndex - startIndex), entry.ChildCount, entry.Children, entry.HandleFunc);
                    entry = new RouteEntry(path.Substring(startIndex, anyIndex - startIndex), 1, children, handleFunc);
                    return;
                }


                var childIndex = -1;
                for (var i = 0; i < entry.ChildCount; i++)
                {
                    if (entry.Children[i].Path[0] == path[index])
                    {
                        index++;
                        remain--;
                        childIndex = i;
                        break;
                    }
                }

                // add entry to entry's children
                if (childIndex < 0)
                {
                    var children = entry.Children;
                    if (entry.Children == null)
                    {
                        children = new RouteEntry[10];
                    }
                    else if (entry.ChildCount == entry.Children.Length)
                    {
                        Array.Resize(ref children, children.Length * 2);
                    }

                    children[entry.ChildCount] = new RouteEntry(path.Substring(index, anyIndex - index), 0, null, handleFunc);
                    entry = new RouteEntry(entry.Path, entry.ChildCount + 1, children, entry.HandleFunc);

                    return;
                }

                entry = ref entry.Children[childIndex];
            }
        }

        public Func<Context, Task> Find(HTTPMethod method, string path)
        {
            path = NormalizePath(path);
            return FindStatic(method, path) ?? FindAny(method, path);
        }

        private Func<Context, Task> FindStatic(HTTPMethod method, string path)
        {
            Dictionary<string, Func<Context, Task>> dict;
            if (!staticHandleFuncDict.TryGetValue(method, out dict)) return null;

            Func<Context, Task> handleFunc;
            if (!dict.TryGetValue(path, out handleFunc)) return null;

            return handleFunc;
        }

        private Func<Context, Task> FindAny(HTTPMethod method, string path)
        {
            BoxedRouteEntry boxedAnyRoot;
            if (!this.anyRootDict.TryGetValue(method, out boxedAnyRoot))
            {
                return null;
            }

            // for match '/abc' to '/abc/*'
            if (path.Length > 1)
            {
                path = path + '/';
            }

            Func<Context, Task> currentHandleFunc = null;
            ref readonly var entry = ref boxedAnyRoot.Entry;

            var index = 1;
            var remain = path.Length - 1;
            while (true)
            {
                var entryPath = entry.Path;
                if (entryPath.Length - 1 > remain)
                {
                    return currentHandleFunc;
                }

                // first char is always same 
                // so iterate 1 to entryPath.Length
                for (var i = 1; i < entryPath.Length; i++, index++)
                {
                    if (entryPath[i] != path[index])
                    {
                        return currentHandleFunc;
                    }
                }

                remain -= (entryPath.Length - 1);
                currentHandleFunc = entry.HandleFunc ?? currentHandleFunc;

                if (remain == 0 || entry.ChildCount == 0)
                {
                    return currentHandleFunc;
                }


                var childIndex = -1;
                for (var i = 0; i < entry.ChildCount; i++)
                {
                    if (entry.Children[i].Path[0] == path[index])
                    {
                        index++;
                        remain--;
                        childIndex = i;
                        break;
                    }
                }

                if (childIndex < 0)
                {
                    return currentHandleFunc;
                }

                entry = ref entry.Children[childIndex];
            }
        }

        private bool IsStaticPath(string path)
            => path.IndexOf('*') < 0;

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
                if (path.Length > 1 && path[path.Length - 1] == '/')
                {
                    path = path.Substring(0, path.Length - 1);
                }
            }

            return path;
        }

        class BoxedRouteEntry
        {
            public RouteEntry Entry;
        }

        readonly struct RouteEntry
        {
            public readonly string Path;
            public readonly int ChildCount;
            public readonly RouteEntry[] Children;
            public readonly Func<Context, Task> HandleFunc;

            public RouteEntry(string path, int childCount, RouteEntry[] children, Func<Context, Task> handleFunc)
            {
                this.Path = path;
                this.ChildCount = childCount;
                this.Children = children;
                this.HandleFunc = handleFunc;
            }
        }
    }
}