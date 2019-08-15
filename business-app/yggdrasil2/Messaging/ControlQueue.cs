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
    public class ControlQueue
    {

        private string BrokerHostname { get; set; }
        private string BrokerUsername { get; set; }
        private string BrokerPassword { get; set; }
        private int BrokerPort { get; set; }

        private string RoutingKey { get; set; }

        private bool StartingUp { get; set; }

        public string TopologyRoutingKey { get; private set; }

        private IConnection connection { get; set; }

        public delegate void OnStartEventHandler(string TopologyQueue);
        public event OnStartEventHandler OnStartCallback;

        public delegate void OnTopologyQueueChangeHandler(string TopologyQueue);
        public event OnTopologyQueueChangeHandler OnTopologyQueueChangeCallback;

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
            var rmqChannel = this.connection.CreateModel();

            rmqChannel.ExchangeDeclare(exchange: "shift_control_exchange", type: "fanout");

            var queueName = rmqChannel.QueueDeclare().QueueName;

            rmqChannel.QueueBind(queue: queueName,
                              exchange: "shift_control_exchange",
                              routingKey: "shift_control_key");

            var consumer = new EventingBasicConsumer(rmqChannel);
            consumer.Received += (model, ea) =>
                {
                    var body = ea.Body;
                    var data = Encoding.UTF8.GetString(body);

                    JObject control_message = JObject.Parse(data);

                    if (ea.BasicProperties.CorrelationId == "links_data")
                    {
                        var is_master = (bool)control_message["is_master"];
                        var routing_key = (string)control_message["routing_key"];

                        if (is_master && !string.IsNullOrWhiteSpace(routing_key))
                        {
                            if (this.StartingUp)
                            {
                                this.StartingUp = false;
                                this.TopologyRoutingKey = routing_key;

                                OnStartCallback?.Invoke(routing_key);
                            }

                            if (routing_key != this.TopologyRoutingKey)
                            {
                                this.TopologyRoutingKey = routing_key;

                                OnTopologyQueueChangeCallback?.DynamicInvoke(routing_key);
                            }
                        } 
                    }
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

        public ControlQueue(string brokerHostname, int brokerPort,
            string brokerUsername, string brokerPassword, string routingKey)
        {
            this.BrokerHostname = brokerHostname;
            this.BrokerPort = brokerPort;
            this.BrokerUsername = brokerUsername;
            this.BrokerPassword = brokerPassword;
            this.RoutingKey = routingKey;

            this.TopologyRoutingKey = string.Empty;
            this.StartingUp = true;
        }
    }
}
    
