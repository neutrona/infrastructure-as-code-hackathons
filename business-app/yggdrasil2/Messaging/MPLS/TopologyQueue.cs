using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#region JSON
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#endregion

#region RabbitMQ
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
#endregion

namespace shift.yggdrasil2.Messaging.MPLS
{
    public class TopologyQueue
    {
        private string BrokerHostname { get; set; }
        private string BrokerUsername { get; set; }
        private string BrokerPassword { get; set; }
        private int BrokerPort { get; set; }

        private string RoutingKey { get; set; }

        private IConnection connection { get; set; }

        public delegate void OnTopologyChangeHandler(JObject data);
        public event OnTopologyChangeHandler OnTopologyChangeCallback;

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
            var rmqChannel = connection.CreateModel();

            rmqChannel.ExchangeDeclare(exchange: "shift_topology_exchange", type: "topic");

            var queueName = rmqChannel.QueueDeclare().QueueName;

            rmqChannel.QueueBind(queue: queueName,
                              exchange: "shift_topology_exchange",
                              routingKey: this.RoutingKey);

            var consumer = new EventingBasicConsumer(rmqChannel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body;
                var data = Encoding.UTF8.GetString(body);

                JObject shift_pcep_message = JObject.Parse(data);

                shift_pcep_message.Add("yggdrasil-data-type", "shift-pcep-message");

                OnTopologyChangeCallback?.Invoke(shift_pcep_message);
            };

            rmqChannel.BasicConsume(queue: queueName, consumer: consumer, autoAck: true);
        }

        public void Disconnect()
        {
            if (this.connection != null && this.connection.IsOpen)
            {
                this.connection.Close();
            }
        }

        public TopologyQueue(string brokerHostname, int brokerPort,
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
