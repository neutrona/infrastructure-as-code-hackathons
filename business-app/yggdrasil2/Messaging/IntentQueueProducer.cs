using System;
using System.Collections.Generic;
using System.Text;

#region JSON
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#endregion

#region RabbitMQ
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
#endregion


namespace shift.yggdrasil2.Messaging
{
    public class IntentQueueProducer
    {
        private string BrokerHostname { get; set; }
        private string BrokerUsername { get; set; }
        private string BrokerPassword { get; set; }
        private int BrokerPort { get; set; }

        private string RoutingKey { get; set; }

        private IConnection connection { get; set; }
        private IModel channel { get; set; }

        private static string exchange_name = "shift_intent_exchange";

        public void Connect()
        {
            if (this.connection != null && this.connection.IsOpen)
            {
                this.connection.Close();
            }

            var factory = new ConnectionFactory()
            {
                HostName = this.BrokerHostname,
                UserName = this.BrokerUsername,
                Password = this.BrokerPassword,
                Port = this.BrokerPort
            };

            this.connection = factory.CreateConnection();
            this.channel = connection.CreateModel();

            this.channel.ExchangeDeclare(exchange: exchange_name, type: "topic", autoDelete: false, arguments: new Dictionary<string, object>() { { "x-ha-policy", "all" } });

            var queueName = this.channel.QueueDeclare(queue: "shift_intent_queue",
                exclusive: false, autoDelete: false, arguments: new Dictionary<string, object>() { { "x-ha-policy", "all" } }).QueueName;

            this.channel.QueueBind(queue: queueName,
                              exchange: exchange_name,
                              routingKey: this.RoutingKey);
        }

        public void Publish(string Message)
        {
            byte[] body = Encoding.UTF8.GetBytes(Message);
            this.channel.BasicPublish(
                exchange: exchange_name,
                routingKey: this.RoutingKey,
                basicProperties:null,
                body: body
                );
        }

        public void Disconnect()
        {
            if (this.connection != null && this.connection.IsOpen)
            {
                this.connection.Close();
            }
        }

        public IntentQueueProducer(string brokerHostname, int brokerPort,
            string brokerUsername, string brokerPassword, string routingKey)
        {
            this.BrokerHostname = brokerHostname;
            this.BrokerPort = brokerPort;
            this.BrokerUsername = brokerUsername;
            this.BrokerPassword = brokerPassword;

            this.RoutingKey = routingKey;
        }
    }
}
