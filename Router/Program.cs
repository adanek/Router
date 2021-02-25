using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Web;

namespace Router {
    class Program {
        static void Main(string[] args)
        {
            var router = new Router();
            router.Route(new Uri("coap://localhost/persons/special?name=Franz&id=17&value=12.3"));
        }
    }

    public class Router {

        private IDictionary<Uri, (Type, MethodInfo)> map;

        public Router()
        {
            this.map = new Dictionary<Uri, (Type, MethodInfo)>();

            var controllers = Assembly.GetExecutingAssembly().GetTypes().Where(t => !t.IsAbstract && typeof(Controller).IsAssignableFrom(t));

            foreach (var controller in controllers)
            {
                var ti = controller.GetTypeInfo();
                var baseroute = ti.GetCustomAttribute<RouteAttribute>();

                var endpoints = ti.GetMethods(BindingFlags.Public | BindingFlags.Instance);
                foreach(var endpoint in endpoints)
                {
                    var route = endpoint.GetCustomAttribute<RouteAttribute>();
                    if (route == null) continue;



                    this.map.Add(new Uri($"/{baseroute.Path}/{route.Path}", UriKind.Relative), (controller, endpoint));
                }
            }
        }



        public void Route(Uri uri)
        {

            var ps = HttpUtility.ParseQueryString(uri.Query);

            var endpoint = this.map[new Uri(uri.LocalPath, UriKind.Relative)];
            var controller = (Controller)Activator.CreateInstance(endpoint.Item1);
            var method = endpoint.Item2;

            var parameters = method.GetParameters();

            var arguments = parameters.Select(p => Convert.ChangeType(ps[p.Name], p.ParameterType, CultureInfo.InvariantCulture)).ToArray();
            method.Invoke(controller, arguments);
        }
    }



    public abstract class Controller {

        public void DoSomething()
        {
            Console.WriteLine($"{this.GetType().Name} does something.");
        }

    }

    [Route("persons")]
    public class PersonController : Controller {

        [Route("special")]
        public void DoSomethingSpecial(string name, int id, double value)
        {
            Console.WriteLine($"{this.GetType().Name} does something special with {name}");
        }
    }

    [Route("rooms")]
    public class RoomController : Controller {

    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class RouteAttribute : Attribute {

        public RouteAttribute(string path)
        {
            this.Path = path;
        }

        public string Path { get; set; }
    }
}
