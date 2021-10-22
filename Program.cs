using Newtonsoft.Json;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Downloader
{
	class Program
	{
		static void Main(string[] args)
		{
			MainAsync().Wait();
		}

		static async Task MainAsync()
		{
			HttpClient client = new HttpClient();
			int count = 0;
			int maxCount = 50;
			var url = "https://support.allizom.org/api/1/kb/";
			var slugs = new List<string>();
			do
			{
				var res = await client.GetAsync(url);
				if (res.IsSuccessStatusCode)
				{
					var json = await res.Content.ReadAsStringAsync();
					dynamic response = JsonConvert.DeserializeObject(json);
					//Console.WriteLine(((Newtonsoft.Json.Linq.JArray)response.results).Count);
					var countToTake = maxCount - count;
					foreach ( var r in response.results)
					{
						slugs.Add((string)r.slug);
						count++;
						if (--countToTake > 0)
							continue;
						break;
					}
					url = response.next;
				}
			} while (count < maxCount);
			Console.WriteLine(slugs.Count);
			foreach(var slug in slugs)
			{
				var res = await client.GetAsync($"https://support.allizom.org/api/1/kb/{slug}");
				if (res.IsSuccessStatusCode)
				{
					var json = await res.Content.ReadAsStringAsync();
					dynamic response = JsonConvert.DeserializeObject(json);
					var html = (string)response.html;
					var strippedImagesHtml = Regex.Replace(html, "<img *?>", "");
					SqliteAdd(slug, strippedImagesHtml);
					Console.WriteLine("Added slug: " + slug);
					await File.WriteAllTextAsync(slug + ".html", strippedImagesHtml);
				}
			}
			Console.ReadKey();
		}

		public static void SqliteAdd(string slug, string html)
		{
			bool needCreateTable = !File.Exists("slugs.db");
			using (var connection = new SQLiteConnection("slugs.db"))
			{
				if( needCreateTable)
				{
					connection.CreateTable<Slugs>();
				}

				var slugs = new Slugs
				{
					Slug = slug,
					Html = html
				};
				connection.Insert(slugs);
			}
		}
	}
	
	public class Slugs
	{
		[PrimaryKey, AutoIncrement]
		public int Id { get; set; }
		public string Slug { get; set; }
		public string Html { get; set; }
	}
}
