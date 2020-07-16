using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ElectionWebApp.Models;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;

namespace ElectionWebApp.Controllers
{
    public class HomeController : Controller
    {
        const string ServiceBusConnectionString = "Endpoint=sb://electionapp.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=pAFmeJJ/gkUBwU5t22iig3G15nYKHMUcK8sldQtPyEY=";
        static IQueueClient queueClient;

        
        private readonly IConfiguration _configuration;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        [Route("api/push")]
        public async void PushMessage(string cityName)
        {
            const int numberOfMessages = 1000;
            queueClient = new QueueClient(ServiceBusConnectionString, $"servicebus{cityName}");

            // Send messages.
            await SendMessagesAsync(numberOfMessages, cityName);
            await queueClient.CloseAsync();
        }

        private async Task SendMessagesAsync(int numberOfMessagesToSend, string cityName)
        {
            try
            {
                var connection = _configuration.GetConnectionString("MasterDatabase");
                connection = connection.Replace("{dbname}", cityName);
                SqlConnection con = new SqlConnection(connection);
                con.Open();
                for (var i = 0; i < numberOfMessagesToSend; i++)
                {
                    // Create a new message to send to the queue
                    string messageBody = $"Mobile Number : {i * 11111}  First Name : first{i}  Last Name : last{i}";
                    var message = new Message(Encoding.UTF8.GetBytes(messageBody));

                    // Send the message to the queue
                    await queueClient.SendAsync(message);
                    SqlCommand cmd = new SqlCommand($"INSERT INTO User_Details(MobileNumber,FirstName,LastName,UniqueId) VALUES (@MobileNumber,@FirstName,@LastName,@UniqueId)", con);
                    cmd.Parameters.AddWithValue("@MobileNumber", i * 11111);
                    cmd.Parameters.AddWithValue("@FirstName", $"first{ i}");
                    cmd.Parameters.AddWithValue("@LastName", $"last{i}");
                    cmd.Parameters.AddWithValue("@UniqueId", new Guid());
                    cmd.ExecuteNonQuery();
                }
                con.Close();
            }
            catch (Exception exception)
            {
                Console.WriteLine($"{DateTime.Now} :: Exception: {exception.Message}");
            }
        }
    }
}
  
