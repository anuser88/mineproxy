using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace mineproxy
{
	class Program
	{
		static async Task Main()
		{
			MP mp = new();
			await mp.Init();
			Console.Write("Threads: ");
			int xdef = 3000;
			int x;
			if (int.TryParse(Console.ReadLine(), out x))
			{
				Console.WriteLine("Threads count set to: " + x);
			}
			else
			{
				Console.WriteLine($"Invalid threads count, fallback to {xdef} threads!");
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
			"https://api.proxyscrape.com/v2/?request=displayproxies&protocol=all&timeout=10000&country=all&simplified=true",
		];
		public static ConcurrentQueue<string> queue = new ConcurrentQueue<string>();
		public static ConcurrentBag<string> live = new ConcurrentBag<string>();
		public List<string> FetchedProxies = new(60000);
		public async Task Init()
		{
			FetchedProxies.Clear();
			foreach (var ProxySource in ProxySources)
			{
				var res = await client.GetAsync(ProxySource);
				if ((int)res.StatusCode == 200)
				{
					string content = await res.Content.ReadAsStringAsync();
					string[] lines = content
						.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

					int c = 0;
					foreach (var line in lines)
					{
						string result = line
    						.Replace("http://", "")
    						.Replace("socks4://", "")
    						.Replace("socks5://", "")
							.Replace("https://", "");
						if (!FetchedProxies.Contains(result)) {
							FetchedProxies.Add(result);
							c++;
						}
					}
					Console.WriteLine($"Fetched {c} proxies!");
				} else 
				{
					Console.WriteLine($"Failed to fetch {ProxySource}!");
				}
			}
			Console.WriteLine($"Fetched {FetchedProxies.Count} proxies in total!");
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

			Console.WriteLine($"\nLIVE TOTAL: {live.Count}");
			System.IO.File.WriteAllLines("live.txt", live);
		}
		public async Task Worker(int id)
		{
			string idvip = id.ToString("D4");
			while (queue.TryDequeue(out string proxy))
			{
				try
				{
					var handler = new HttpClientHandler
					{
						Proxy = new WebProxy(proxy),
						UseProxy = true
					};

					using var proxiedClient = new HttpClient(handler);
					proxiedClient.Timeout = TimeSpan.FromSeconds(5);

					var res = await proxiedClient.GetStringAsync("https://httpbin.org/ip");

					live.Add(proxy);
					Console.WriteLine($"[WORKER {idvip}] LIVE {proxy}");
				}
				catch
				{
					Console.WriteLine($"[WORKER {idvip}] DEAD {proxy}");
				}
			}
		}
	}
}