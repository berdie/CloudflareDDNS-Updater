using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CloudflareDDNS_Updater
{
    public class CloudflareConfig
    {
        public string ApiKey { get; set; }
        public string Email { get; set; }
        public string Domain { get; set; }
    }

    public class CloudflareUpdater
    {
        private const string apiUrl = "https://api.cloudflare.com/client/v4/";
        private readonly string apiKey;
        private readonly string email;
        private readonly HttpClient httpClient;

        public CloudflareUpdater(string apiKey, string email)
        {
            this.apiKey = apiKey;
            this.email = email;
            this.httpClient = new HttpClient();
            this.httpClient.DefaultRequestHeaders.Add("X-Auth-Email", email);
            this.httpClient.DefaultRequestHeaders.Add("X-Auth-Key", apiKey);
        }

        public async Task<string> GetPublicIpAsync()
        {
            string[] services = {
                    "https://ipv4.icanhazip.com/",
                    "https://api.ipify.org/"
                };

            foreach (var url in services)
            {
                try
                {
                    var response = await httpClient.GetStringAsync(url);
                    if (!string.IsNullOrWhiteSpace(response))
                        return response.Trim();
                }
                catch
                {
                    // Prova il prossimo servizio
                }
            }

            throw new Exception("Impossibile ottenere l'indirizzo IP pubblico.");
        }

        public async Task<string> GetZoneIdAsync(string domain)
        {
            var response = await httpClient.GetAsync($"{apiUrl}zones?name={domain}");
            var content = await response.Content.ReadAsStringAsync();

            var json = JObject.Parse(content);

            Console.WriteLine(content);
            if (json["result"] is JArray resultArray && resultArray.Count > 0)
            {
                var zoneId = resultArray[0]?["id"]?.ToString();
                if (string.IsNullOrEmpty(zoneId)) throw new Exception("Zone ID non trovato.");
                return zoneId;
            }
            throw new Exception("Invalid or empty result from API.");
        }

        public async Task UpdateAllARecordsAsync(string domain)
        {
            string newIp = await GetPublicIpAsync();
            string zoneId = await GetZoneIdAsync(domain);

            var response = await httpClient.GetAsync($"{apiUrl}zones/{zoneId}/dns_records?type=A");
            var content = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(content);
            var resultArray = json["result"] as JArray;
            if (resultArray == null || !resultArray.Any())
                throw new Exception("Invalid or empty result from API.");

            var records = json["result"];

            foreach (var record in records)
            {
                string recordId = record["id"]?.ToString();
                string recordName = record["name"]?.ToString();

                if (string.IsNullOrEmpty(recordId) || string.IsNullOrEmpty(recordName))
                    continue;

                var updateData = new
                {
                    type = "A",
                    name = recordName,
                    content = newIp,
                    ttl = 1,
                    proxied = (bool?)record["proxied"] ?? false
                };

                var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(updateData);
                var updateContent = new StringContent(jsonData, Encoding.UTF8, "application/json");

                var updateUrl = $"{apiUrl}/zones/{zoneId}/dns_records/{recordId}";
                var updateResponse = await httpClient.PutAsync(updateUrl, updateContent);
                var updateResult = await updateResponse.Content.ReadAsStringAsync();

                if (updateResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine($"✅ Aggiornato: {recordName} → {newIp}");
                }
                else
                {
                    Console.WriteLine($"❌ Errore su {recordName}: {updateResult}");
                }
            }
        }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            CloudflareConfig config;
            string configPath = "config.json";

            // Verifica se è stato specificato un percorso del file di configurazione
            if (args.Length > 0)
                configPath = args[0];

            try
            {
                // Legge il file di configurazione
                if (!File.Exists(configPath))
                {
                    Console.WriteLine($"File di configurazione non trovato: {configPath}");
                    Console.WriteLine("Creazione di un file di configurazione di esempio...");

                    // Crea un file di configurazione di esempio
                    var exampleConfig = new CloudflareConfig
                    {
                        ApiKey = "inserisci-la-tua-api-key-qui",
                        Email = "tua-email@esempio.com",
                        Domain = "tuo-dominio.com"
                    };

                    File.WriteAllText(configPath, JsonConvert.SerializeObject(exampleConfig, Formatting.Indented));

                    Console.WriteLine($"Esempio di file di configurazione creato: {configPath}");
                    Console.WriteLine("Modifica il file con i tuoi dati e riavvia il programma.");
                    return;
                }

                string json = File.ReadAllText(configPath);
                config = JsonConvert.DeserializeObject<CloudflareConfig>(json);

                if (string.IsNullOrEmpty(config.ApiKey) || string.IsNullOrEmpty(config.Email) || string.IsNullOrEmpty(config.Domain))
                {
                    Console.WriteLine("Configurazione incompleta. Assicurati di specificare ApiKey, Email e Domain nel file di configurazione.");
                    return;
                }

                var updater = new CloudflareUpdater(config.ApiKey, config.Email);
                await updater.UpdateAllARecordsAsync(config.Domain);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore: {ex.Message}");
            }
        }
    }
}


