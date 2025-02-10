using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using OpenHardwareMonitor.Hardware;
using System;
using System.Net;
using System.Text;
using System.Reflection;
using System.Linq;
using System.Threading.Tasks;

namespace OpenHardwareMonitorJsonServer
{
    // 这是从 OpenHardwareMonitor 的 GUI 项目中复制粘贴的 Visitor 类
    // 用于遍历计算机硬件树的访问者（Visitor）模式实现
    class Visitor : IVisitor
    {
        // 访问整个计算机节点，遍历所有硬件
        public void VisitComputer(IComputer computer)
        {
            // 调用 Traverse 方法（类似于递归遍历），传入当前的 Visitor 实例
            computer.Traverse(this);
        }

        // 访问单个硬件节点
        public void VisitHardware(IHardware hardware)
        {
            // 更新硬件状态，类似于刷新或采集最新数据
            hardware.Update();

            // 遍历该硬件的所有子硬件（例如：主板上的多个芯片）
            foreach (IHardware subHardware in hardware.SubHardware)
            {
                // 对每个子硬件继续应用 Visitor 模式
                subHardware.Accept(this);
            }
        }

        // 对于参数节点不做任何处理
        public void VisitParameter(IParameter parameter) { }

        // 对于传感器节点不做任何处理
        public void VisitSensor(ISensor sensor) { }
    }

    // Json.NET 自定义转换器，用于将对象转换为其 ToString() 返回的字符串
    // 对于 Java 开发者来说，这类似于重写对象的 toString() 方法用于 JSON 输出
    class ToStringJsonConverter : JsonConverter
    {
        // 这里简单返回 true，表示可以转换任何类型
        public override bool CanConvert(Type objectType)
        {
            return true;
        }

        // 反序列化时不实现该方法（此示例仅用于序列化输出）
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        // 将对象转换为字符串后写入 JSON 输出
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }
    }

    // Json.NET 自定义合约解析器
    // 用于定制 JSON 序列化的行为，由于无法修改已编译库的属性注解，所以采用这种方式
    class CustomContractResolver : DefaultContractResolver
    {
        // 重写属性创建方法，可以控制哪些属性被序列化
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            // 调用父类方法创建 JsonProperty 对象
            JsonProperty property = base.CreateProperty(member, memberSerialization);

            // 不序列化 Values 或 Parameters 属性
            // Values 属性包含过去24小时所有传感器的读数，数据量可能非常大
            // Parameters 属性仅包含计算 Value 属性的说明字符串，通常不需要输出
            if (property.PropertyName == "Values" || property.PropertyName == "Parameters")
            {
                // 设置序列化时跳过该属性
                property.ShouldSerialize = (x) => false;
            }

            return property;
        }

        // 重写对象合约创建方法，可对整个对象应用自定义转换器
        protected override JsonObjectContract CreateObjectContract(Type objectType)
        {
            JsonObjectContract contract = base.CreateObjectContract(objectType);
            // 如果对象类型为 OpenHardwareMonitor.Hardware.Identifier，则使用自定义的 ToStringJsonConverter
            // 否则，默认序列化会将其视为空对象（因为没有公共属性），输出类似 "Identifier: {}"
            if (objectType == typeof(Identifier))
            {
                contract.Converter = new ToStringJsonConverter();
            }

            return contract;
        }
    }

    class Program
    {
        public static void Main(string[] args)
        {
            var port = 8080;


            // 检查是否提供了 -p
            var portArg = args.FirstOrDefault(arg => arg.StartsWith("-p"));
            if (portArg != null)
            {
                // 尝试解析跟在 -p 后面的端口号
                var portValue = args.SkipWhile(arg => arg != portArg).Skip(1).FirstOrDefault();
                if (!string.IsNullOrEmpty(portValue) && int.TryParse(portValue, out int providedPort))
                {
                    port = providedPort;
                }
            }

            // 检查是否提供了 help
            if (args.FirstOrDefault(arg => arg.StartsWith("help")) != null || args.FirstOrDefault(arg => arg.StartsWith("-help")) != null)
            {
                Console.WriteLine("参数：");
                Console.WriteLine("-p           指定WEB端口并开启服务器(默认8080)");
                Console.WriteLine("--console    以控制台Json格式返回硬件信息（默认禁用并且与WEB方式冲突）");
                Console.WriteLine("--group      以群组格式返回硬件信息（默认开启）");
                Console.WriteLine("--mainboard  启用主板监控 (默认启用)");
                Console.WriteLine("--cpu        启用CPU监控 (默认启用)");
                Console.WriteLine("--ram        启用内存监控 (默认启用)");
                Console.WriteLine("--gpu        启用GPU监控 (默认启用)");
                Console.WriteLine("--fan        启用风扇控制器监控 (默认启用)");
                Console.WriteLine("--hdd        启用硬盘监控 (默认启用)");
                return;
            }


            var isConsole = GetBoolArgValue(args, "--console", false);
            var isGroup = GetBoolArgValue(args, "--group", true);
            // 默认硬件监控设置
            var mainboardEnabled = GetBoolArgValue(args, "--mainboard", true);
            var cpuEnabled = GetBoolArgValue(args, "--cpu", true);
            var ramEnabled = GetBoolArgValue(args, "--ram", true);
            var gpuEnabled = GetBoolArgValue(args, "--gpu", true);
            var fanControllerEnabled = GetBoolArgValue(args, "--fan", true);
            var hddEnabled = GetBoolArgValue(args, "--hdd", true);

            // 创建一个 Computer 实例，并启用所有硬件监控传感器
            var computer = new Computer()
            {
                MainboardEnabled = true,
                CPUEnabled = true,
                RAMEnabled = true,
                GPUEnabled = true,
                FanControllerEnabled = true,
                HDDEnabled = true,
            };

            
            if (isConsole)  // 是否控制台输出 JSON
            {
                // 初始化硬件传感器（类似于打开设备资源）
                computer.Open();
                // 如果不开启服务器则返回内容
                Console.WriteLine(getHardware(computer, new Visitor(), isGroup));
            }
            else if (port >= 0) // 如果提供了端口则说明需要开启服务器
            {
                Console.Write("初始化硬件传感器...");
                // 初始化硬件传感器（类似于打开设备资源）
                computer.Open();
                Console.Write(" OK\n");
                // 启动 HTTP 服务器，监听传入的请求
                Task.Run(() => StartServer(computer, port, isGroup)).Wait();
            }
            else {
                Console.WriteLine("输入 Help 来获取帮助");
            }

        }

        private static bool GetBoolArgValue(string[] args, string argName, bool defaultValue)
        {
            var arg = args.FirstOrDefault(a => a.StartsWith(argName));
            if (arg == null) return defaultValue; // 如果没有找到参数，则使用默认值
            if (arg.Length > argName.Length + 1) // 检查是否提供了参数值
            {
                return bool.Parse(arg.Substring(argName.Length + 1));
            }
            return true; // 如果只提供了参数名，没有提供值，默认启用
        }

        private static string getHardware(Computer computer, Visitor visitor, bool isGroup = true) {
            // 通过 Visitor 模式更新所有硬件传感器数据
            computer.Accept(visitor);

            // 使用 LINQ 将 computer.Hardware 按硬件类型分组，
            // 分组后的字典的 key 为硬件类型名称（例如 "CPU"），
            // value 为对应类型的硬件对象列表
            var groupedHardware = computer.Hardware
                .GroupBy(h => h.HardwareType.ToString())
                .ToDictionary(g => g.Key, g => g.ToList());

            // 序列化时设置忽略循环引用和使用自定义合约解析器
            var settings = new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                ContractResolver = new CustomContractResolver()
            };

            if (isGroup)
            {
                // 将分组后的字典序列化为 JSON 字符串
                return JsonConvert.SerializeObject(groupedHardware, settings);
            }
            else 
            {
                return JsonConvert.SerializeObject(computer.Hardware, settings);
            }
        }

        private async static Task StartServer(Computer computer, int port,bool isGroup)
        {
            // 创建一个 Visitor 实例，用于更新硬件数据
            var visitor = new Visitor();

            // 创建 HTTP 服务器监听器，监听指定端口（支持任意IP地址）
            var listener = new HttpListener();
            listener.Prefixes.Add($"http://+:{port}/");
            listener.Start();
            Console.WriteLine($"监听地址: http://localhost:{port}");
            // 当服务器处于监听状态时，持续等待并处理请求
            while (listener.IsListening)
            {
                // 异步等待客户端请求
                var context = await listener.GetContextAsync();

                // 将分组后的字典序列化为 JSON 字符串
                var data = getHardware(computer,visitor, isGroup);

                // 将 JSON 数据转换为 UTF-8 编码的字节数组
                var buffer = Encoding.UTF8.GetBytes(data);
                var response = context.Response;

                try
                {
                    // 设置跨域访问头，允许任意域访问
                    response.AddHeader("Access-Control-Allow-Origin", "*");
                    // 指定内容类型为 JSON
                    response.AddHeader("Content-Type", "application/json");
                    // 将序列化的 JSON 数据写入响应流
                    response.OutputStream.Write(buffer, 0, buffer.Length);
                }
                catch (HttpListenerException)
                {
                    // 捕获 HttpListenerException 异常，不做处理
                    // 这通常是由于客户端提前关闭了连接
                }

                // 关闭响应流，释放资源
                response.OutputStream.Close();
            }
        }

    }
}
