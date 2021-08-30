using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using CostManagementDBFiller.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace CostManagementDBFiller
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var response = GetSubscription().Result;
            Console.WriteLine("Ingrese la fecha:");
            var fecha = Console.ReadLine();
            await Run(response, fecha);
        }

        private static async Task Run(ICollection<Subscription> response, string fecha)
        {
            foreach (var subscription in response)
            {
                var actualSubscription = await GetFromShaina(subscription.subscriptionId);
                var date = Convert.ToDateTime(fecha);
                var fromDate = date;
                var toDate = fromDate.AddDays(10);
                while(toDate <= DateTime.Now)
                {
                    if (toDate > DateTime.Now)
                    {
                        toDate = DateTime.Now;
                    }
                    var hey = await executeAzureFunction(actualSubscription.Value, fromDate, toDate, subscription.subscriptionId);
                    Console.WriteLine($"Subscripcion {subscription.subscriptionId} de {subscription.clientApp.appName}");
                    Console.WriteLine($"Desde {fromDate.ToShortDateString()} hasta {toDate.ToShortDateString()}");
                    Console.WriteLine($"Resultado: {hey}");
                    fromDate = toDate.AddDays(1);
                    toDate = fromDate.AddDays(10);
                    if (fromDate > DateTime.Now)
                    {
                        fromDate = DateTime.Now;
                        toDate = DateTime.Now;
                    }
                    else if (fromDate == DateTime.Now)
                    {
                        toDate = DateTime.Now.AddDays(1);
                    }
                }
                Console.WriteLine("=================================================================");
            }
            Console.WriteLine("Finished");
            Console.Read();
        }

        private static async Task<ICollection<Subscription>> GetSubscription()
        {
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", "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");
            var response =  await httpClient.GetAsync("https://sxcostmanagementapi.azurewebsites.net/api/Subscription/details");
            var result = await response.Content.ReadAsAsync <ICollection<Subscription>>();

            return result;
        }

        private static async Task<KeyVaultSecret> GetFromShaina(string subscriptionId)
        {
            Environment.SetEnvironmentVariable("AZURE_CLIENT_ID", "b5f7e2d2-aff2-4fa2-be4c-d98cbd1c3bac");
            Environment.SetEnvironmentVariable("AZURE_TENANT_ID", "9acf6dd6-1978-4d9c-9a9c-c9be95245565");
            Environment.SetEnvironmentVariable("AZURE_CLIENT_SECRET", "QE0hXg3qgN-vNQi0Qd~V~8rK~4M.-Ufs2x");
            var client = new SecretClient(vaultUri: new Uri("https://shaina.vault.azure.net/"), new DefaultAzureCredential());

            return client.GetSecret(subscriptionId);
        }

        private static async Task<string> executeAzureFunction(string credentials, DateTime fromDate, DateTime toDate, string subscriptionId)
        {
            HttpClient httpClient = new HttpClient();
            httpClient.Timeout = new TimeSpan(0, 15, 0);
            httpClient.DefaultRequestHeaders.Add("fromDate", fromDate.ToShortDateString());
            httpClient.DefaultRequestHeaders.Add("toDate", toDate.ToShortDateString());
            httpClient.DefaultRequestHeaders.Add("subscriptionId", subscriptionId);
            Credential credential = JsonConvert.DeserializeObject<Credential>(credentials);
            var serializedCredential = JsonConvert.SerializeObject(credential);
            var stringContent = new StringContent(serializedCredential, Encoding.UTF8, "application/Json");
            var result = await httpClient.PostAsync("http://localhost:7071/api/DailyConsumption", stringContent);
            if (result.StatusCode == System.Net.HttpStatusCode.OK)
            return await result.Content.ReadAsStringAsync();
            

            return $"No funciono.";
        }
    }
}
