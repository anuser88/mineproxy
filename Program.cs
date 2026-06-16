using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;

namespace mineproxy
{
	class Program
	{
		static async Task Main()
		{
			MP mp = new();
			await mp.Init();
			int xdef = 5000;
			Console.Write($"Threads(default value is {xdef}): ");
			int x;
			if (int.TryParse(Console.ReadLine(), out x))
			{
				Console.WriteLine("Threads count set to: " + x);
			}
			else
			{
				Console.WriteLine($"\nInvalid threads count, fallback to {xdef} threads!");
				x = xdef;
			}
			await mp.Run(x);
		}
	}
	public class MP
	{
		public HttpClient client = new();
		public static string[] ProxySources = [
			"https://raw.githubusercontent.com/TheSpeedX/PROXY-List/master/http.txt",
			"https://raw.githubusercontent.com/TheSpeedX/PROXY-List/master/socks4.txt",
			"https://raw.githubusercontent.com/TheSpeedX/PROXY-List/master/socks5.txt",
			"https://raw.githubusercontent.com/jetkai/proxy-list/main/online-proxies/txt/proxies.txt",
			"https://raw.githubusercontent.com/monosans/proxy-list/main/proxies/all.txt", //Linus Torvalds
			"https://raw.githubusercontent.com/roosterkid/openproxylist/main/HTTPS_RAW.txt",
			"https://raw.githubusercontent.com/almroot/proxylist/master/list.txt",
			"https://raw.githubusercontent.com/ShiftyTR/Proxy-List/master/proxy.txt",
			"https://raw.githubusercontent.com/hookzof/socks5_list/master/proxy.txt",
			"https://raw.githubusercontent.com/clarketm/proxy-list/master/proxy-list-raw.txt",
			"https://raw.githubusercontent.com/proxifly/free-proxy-list/main/proxies/all/data.txt",
			"https://raw.githubusercontent.com/ALIILAPRO/Proxy/main/http.txt",
			"https://raw.githubusercontent.com/ALIILAPRO/Proxy/main/socks4.txt",
			"https://raw.githubusercontent.com/ALIILAPRO/Proxy/main/socks5.txt",
			"https://raw.githubusercontent.com/Zaeem20/FREE_PROXIES_LIST/master/http.txt",
			"https://raw.githubusercontent.com/Zaeem20/FREE_PROXIES_LIST/master/https.txt",
			"https://raw.githubusercontent.com/Zaeem20/FREE_PROXIES_LIST/master/socks4.txt",
			"https://raw.githubusercontent.com/Zaeem20/FREE_PROXIES_LIST/master/socks5.txt",
			"https://raw.githubusercontent.com/vakhov/fresh-proxy-list/master/proxylist.txt",
			"https://raw.githubusercontent.com/r00tee/Proxy-List/main/Https.txt",
			"https://raw.githubusercontent.com/r00tee/Proxy-List/main/Socks4.txt",
			"https://raw.githubusercontent.com/r00tee/Proxy-List/main/Socks5.txt",
			"https://github.com/databay-labs/free-proxy-list/raw/master/http.txt",
			"https://github.com/databay-labs/free-proxy-list/raw/master/socks4.txt",
			"https://github.com/databay-labs/free-proxy-list/raw/master/socks5.txt",
			"https://github.com/elliottophellia/proxylist/raw/master/results/mix_checked.txt",
			"https://github.com/rdavydov/proxy-list/raw/main/proxies/http.txt",
			"https://github.com/rdavydov/proxy-list/raw/main/proxies/socks4.txt",
			"https://github.com/rdavydov/proxy-list/raw/main/proxies/socks5.txt",
			"https://github.com/prxchk/proxy-list/raw/main/all.txt",
			"https://github.com/iplocate/free-proxy-list/raw/refs/heads/main/all-proxies.txt",
			"https://api.proxyscrape.com/v2/?request=displayproxies&protocol=all&timeout=10000&country=all&simplified=true",
		];
		public static ConcurrentQueue<string> queue = new ConcurrentQueue<string>();
		public static ConcurrentBag<string> live = new ConcurrentBag<string>();
		public List<string> FetchedProxies = new(70000);
		public async Task Init()
		{
			int c;
			string content;
			FetchedProxies.Clear();
			foreach (var ProxySource in ProxySources)
			{
				var res = await client.GetAsync(ProxySource);
				if ((int)res.StatusCode == 200)
				{
					content = await res.Content.ReadAsStringAsync();
					string[] lines = content
						.Replace("http://", "")
    					.Replace("socks4://", "")
    					.Replace("socks5://", "")
						.Replace("https://", "")
						.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
					Console.WriteLine($"\n\x1b[33mFetching {lines.Length} proxies...\x1b[0m");
					c = 0;
					foreach (var line in lines)
					{	
						if (!FetchedProxies.Contains(line)) {
							FetchedProxies.Add(line);
							c++;
						}
					}
					Console.WriteLine($"\x1b[32mFetched {c} proxies!\x1b[0m");
				} else 
				{
					Console.WriteLine($"\x1b[31mFailed to fetch {ProxySource}!\x1b[0m");
				}
			}
			await this.Geonode();
			await this.Proxiware();
			Console.WriteLine($"\n\x1b[33mFetched {FetchedProxies.Count} proxies in total!\x1b[0m");
		}
		public async Task Run(int workerCount)
		{
			foreach (var p in FetchedProxies)
				queue.Enqueue(p);

			Task[] workers = new Task[workerCount];

			for (int i = 0; i < workerCount; i++)
			{
				workers[i] = Worker(i);
			}

			await Task.WhenAll(workers);

			Console.WriteLine($"\n\x1b[33mLIVE TOTAL: \x1b[1m{live.Count}\x1b[0m");
			System.IO.File.WriteAllLines("live.txt", live);
		}
		public async Task Worker(int id)
		{
			string blacklist = "206.123.156";
			string idvip = id.ToString("D4");
			while (queue.TryDequeue(out string proxy))
			{
				if (proxy.StartsWith(blacklist)) continue;
				try
				{
					var handler = new HttpClientHandler
					{
						Proxy = new WebProxy(proxy),
						UseProxy = true
					};

					using var proxiedClient = new HttpClient(handler);
					proxiedClient.Timeout = TimeSpan.FromSeconds(10);

					var res = await proxiedClient.GetStringAsync("https://httpbin.org/ip");

					live.Add(proxy);
					Console.WriteLine($"\x1b[32m[WORKER {idvip}] LIVE {proxy}\x1b[0m");
				}
				catch
				{
					Console.WriteLine($"\x1b[31m[WORKER {idvip}] DEAD {proxy}\x1b[0m");
				}
			}
		}
		public async Task Geonode()
		{
			int c = 0;
			string ip;
			string port;
			var res = await client.GetAsync("https://proxylist.geonode.com/api/proxy-list?limit=500");
			if ((int)res.StatusCode == 200)
			{
				string content = await res.Content.ReadAsStringAsync();
				JsonDocument doc = JsonDocument.Parse(content);
				JsonElement data = doc.RootElement.GetProperty("data");
				Console.WriteLine($"\n\x1b[33mFetching {data.GetArrayLength()} proxies...\x1b[0m");
				foreach (JsonElement line in data.EnumerateArray())
				{
					ip = line.GetProperty("ip").GetString();
					port = line.GetProperty("port").GetString();
					if (!FetchedProxies.Contains($"{ip}:{port}")) {
						FetchedProxies.Add($"{ip}:{port}");
						c++;
					}
				}
				Console.WriteLine($"\x1b[32mFetched {c} proxies!\x1b[0m");
			} else 
			{
				Console.WriteLine("\x1b[31mFailed to fetch GeoNode!\x1b[0m");
			}
			c = 0;
			res = await client.GetAsync("https://freeproxies-api.website.proxymaven.com/proxies?per_page=100000");
			if ((int)res.StatusCode == 200)
			{
				string content = await res.Content.ReadAsStringAsync();
				JsonDocument doc = JsonDocument.Parse(content);
				JsonElement data = doc.RootElement.GetProperty("proxies");
				Console.WriteLine($"\n\x1b[33mFetching {data.GetArrayLength()} proxies...\x1b[0m");
				foreach (JsonElement line in data.EnumerateArray())
				{
					ip = line.GetProperty("proxy").GetString();
					if (!FetchedProxies.Contains(ip)) {
						FetchedProxies.Add(ip);
						c++;
					}
				}
				Console.WriteLine($"\x1b[32mFetched {c} proxies!\x1b[0m");
			} else 
			{
				Console.WriteLine("\x1b[31mFailed to fetch GeoNode!\x1b[0m");
			}
		}
		public async Task Proxiware()
		{
			string content;
			string src;
			string ip;
			int port;
			int c;
			for (int i = 1; i < 118; i++) //118
			{
				src = $"https://papi.proxiware.com/proxies?page={i}";
				var res = await client.GetAsync(src);
				if ((int)res.StatusCode == 200)
				{
					content = await res.Content.ReadAsStringAsync();
					JsonDocument doc = JsonDocument.Parse(content);
					JsonElement data = doc.RootElement.GetProperty("proxies");
					c = 0;
					Console.WriteLine($"\n\x1b[33mFetching {data.GetArrayLength()} proxies...\x1b[0m");
					foreach (JsonElement line in data.EnumerateArray())
					{
						ip = line.GetProperty("addr").GetString();
						port = line.GetProperty("port").GetInt32();
						if (!FetchedProxies.Contains($"{ip}:{port}")) {
							FetchedProxies.Add($"{ip}:{port}");
							c++;
						}
					}
					Console.WriteLine($"\x1b[32mFetched {c} proxies!\x1b[0m");
				} else 
				{
					Console.WriteLine("\x1b[31mFailed to fetch GeoNode!\x1b[0m");
				}
			}
		}
	}
}