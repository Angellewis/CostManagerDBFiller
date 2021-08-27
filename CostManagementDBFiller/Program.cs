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
        static void Main(string[] args)
        {
            var response = GetSubscription().Result;
            Console.WriteLine("Ingrese la fecha:");
            var fecha = Console.ReadLine();

            foreach (var subscription in response)
            {
                var actualSubscription = GetFromShaina(subscription.subscriptionId).Result;
                Credential credential = JsonConvert.DeserializeObject<Credential>(actualSubscription.Value);
                var serializedCredential = JsonConvert.SerializeObject(credential);
                var date = Convert.ToDateTime(fecha);
                var fromDate = date;
                for (int i = date.Day; i <= DateTime.Now.Day; i ++)
                {
                    var toDate = fromDate.AddDays(10);
                    var hey = executeAzureFunction(serializedCredential, fromDate, toDate, subscription.subscriptionId).Result;
                    fromDate = toDate.AddDays(1);
                }
            }
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
            httpClient.DefaultRequestHeaders.Add("fromDate", fromDate.ToString());
            httpClient.DefaultRequestHeaders.Add("toDate", toDate.ToString());
            httpClient.DefaultRequestHeaders.Add("subscriptionId", subscriptionId);
            var json = JsonConvert.SerializeObject(credentials, Formatting.Indented);
            var stringContent = new StringContent(json, Encoding.UTF8, "application/Json");
            var result = await httpClient.PostAsync("https://dailyconsumev2.azurewebsites.net/api/DailyConsumption?code=eJHgDN3R8aoa6ORifccuVv3gYzqUe4t0nP1GHtBC5xiv5QHq1TSdew==", stringContent);
            return await result.Content.ReadAsStringAsync();
        }
    }
}
