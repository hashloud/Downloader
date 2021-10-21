using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
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
		static void Main()
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
			int id = 0;
			foreach(var slug in slugs)
			{
				var res = await client.GetAsync($"https://support.allizom.org/api/1/kb/{slug}");
				var json = await res.Content.ReadAsStringAsync();
				dynamic response = JsonConvert.DeserializeObject(json);
				var html = (string)response.html;
				var strippedImagesHtml = Regex.Replace(html, "<img *?>", "");
				Sqlite(id++, slug, strippedImagesHtml);
			}
			Console.ReadKey();
		}

		public static void Sqlite(int id, string slug, string html)
		{
			bool needCreateTable = !File.Exists("hello.db");
			using (var connection = new SqliteConnection("Data Source=hello.db"))
			{
				connection.Open();

				if( needCreateTable)
				{
					var createCommand = connection.CreateCommand();
					createCommand.CommandText =	@"CREATE TABLE hello (id int PRIMARY KEY, slug varchar(255) NOT NULL, html text)";
					createCommand.ExecuteNonQuery();
				}

				var command = connection.CreateCommand();
				command.CommandText = $"INSERT hello (id={id}, slug='{slug}', html='{html}')";
				//command.Parameters.AddWithValue("$id", id);
				command.ExecuteNonQuery();
			}
		}
	}
}
