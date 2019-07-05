using System;
using System.Threading.Tasks;
using Mochi;
using Xunit;

namespace Mochi.Test
{
    public class RouterTest
    {
        [Fact]
        public void StaticRouteTest()
        {
            var router = new Router();
            TestRoute(router, HTTPMethod.Get, "/", "/");
            TestRoute(router, HTTPMethod.Get, "/hoge", "/hoge");
            TestRoute(router, HTTPMethod.Get, "/fuga/", "/fuga/");
            TestRoute(router, HTTPMethod.Get, "/piyo", "/piyo");
        }

        [Fact]
        public void AnyRouteTest()
        {
            var router = new Router();
            var root = RouteTester.Create(router, HTTPMethod.Get, "/*");
            root.Route("", "/", "/abc", "/eee", "/abc/efg");

            var case1 = RouteTester.Create(router, HTTPMethod.Get, "/abcd*");
            var case2 = RouteTester.Create(router, HTTPMethod.Get, "/abcdefgh*");
            var case3 = RouteTester.Create(router, HTTPMethod.Get, "/acc*");
            var case4 = RouteTester.Create(router, HTTPMethod.Get, "/zzz*");

            root.Route("", "/", "/a", "/b");
            root.NotRoute("/abcdefg", "/zzzzzz");

            case1.Route("/abcd", "/abcd/", "/abcdefg", "/abcd/hoge");
            case1.NotRoute("/", "/abc", "/k");

            case2.Route("/abcdefgh", "/abcdefgh/", "/abcdefghijk", "/abcdefghijk/l", "/abcdefgh/ijk");
            case2.NotRoute("/", "/abcdfgh", "/k");

            case3.Route("/acc", "/acc/", "/acckk", "/acc/k");
            case3.NotRoute("/", "/abc", "/abcd");

            case4.Route("/zzz", "/zzz/", "/zzzb");
            case4.NotRoute("/", "/acc", "/abcd");
        }

        private static void TestRoute(Router router, HTTPMethod method, string registerPath, params string[] actualPaths)
            => RouteTester.Create(router, method, registerPath).Route(actualPaths);

        class RouteTester
        {
            public HTTPMethod Method { get; }
            public string RegisterPath { get; }
            private Router router;
            private Func<Context, Task> handleFunc;
            private bool handleFuncIsCalled;

            public static RouteTester Create(Router router, HTTPMethod method, string path)
                => new RouteTester(router, method, path);

            private RouteTester(Router router, HTTPMethod method, string path)
            {
                this.router = router;
                this.handleFunc = _ =>
                {
                    handleFuncIsCalled = true;
                    return default;
                };
                this.router.Register(method, path, this.handleFunc);
                this.Method = method;
                this.RegisterPath = path;
            }

            public void Route(params string[] paths)
            {
                foreach (var path in paths)
                {
                    Assert.True(Test(path), $"route failed. Method:'{this.Method}', Register:'{this.RegisterPath}', Actual:'{path}'");
                }
            }

            public void NotRoute(params string[] paths)
            {
                foreach (var path in paths)
                {
                    Assert.False(Test(path), $"invalid route. Method:'{this.Method}', Register:'{this.RegisterPath}', Actual:'{path}'");
                }
            }

            public bool Test(string path)
            {
                handleFuncIsCalled = false;
                var handleFunc = this.router.Find(this.Method, path);
                _ = handleFunc(default);
                return handleFuncIsCalled;
            }
        }
    }
}
