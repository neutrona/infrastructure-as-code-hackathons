using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#region JSON.NET
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
#endregion

#region SSH.NET
using Renci.SshNet;
#endregion

#region Grapevine
using Grapevine.Server;
using Grapevine.Server.Attributes;
using Grapevine.Interfaces.Server;
using Grapevine.Shared;
#endregion

namespace shift_netconf_broker
{
    class Program
    {
        static Config config = Config.getConfig();
        static InventoryClient inventoryClient = new InventoryClient(config.Inventory.InventoryURI,
                config.Inventory.InventoryResource,
                config.Inventory.InventoryLimit,
                config.Inventory.InventoryAuth.Username,
                config.Inventory.InventoryAuth.Password,
                config.Inventory.InventoryBlackListRegex,
                config.Inventory.InventoryReloadInterval,
                config.Inventory.NodeAuth.Username,
                config.Inventory.NodeAuth.Password,
                config.Inventory.NodeKeepAliveInterval,
                config.MessageBroker.MessageBrokerHost,
                config.MessageBroker.MessageBrokerUsername,
                config.MessageBroker.MessageBrokerPassword,
                config.MessageBroker.ExchangeName,
                config.MessageBroker.RPCRequestQueueSuffix,
                config.MessageBroker.RPCRoutingKeyPrefix,
                config);

        private static System.Timers.Timer InventoryReloadTimer = new System.Timers.Timer(config.Inventory.InventoryReloadInterval);

        static void Main(string[] args)
        {

            if (config.Inventory.LoadNodesOnStartUp)
            {
                Console.WriteLine("Loading Inventory...");
                inventoryClient.LoadInventory();
            }

            using (var server = new RestServer())
            {
                server.Host = config.RESTServerConfig.Host;
                server.Port = config.RESTServerConfig.Port.ToString();
                server.Advanced.AuthenticationSchemes = System.Net.AuthenticationSchemes.Basic;
                server.LogToConsole().Start();

                if (config.Inventory.LoadNodesOnStartUp)
                {
                    Console.WriteLine("Connecting Inventory...");
                    inventoryClient.ConnectInventory();


                    // Inventory Reload
                    InventoryReloadTimer.Elapsed += (s, ea) => {
                        inventoryClient.LoadInventory();
                        inventoryClient.ConnectInventory();
                    };

                    InventoryReloadTimer.Enabled = true;
                }

                Console.ReadLine();

                server.Stop();
                Console.WriteLine("Disconnecting Inventory...");
                InventoryReloadTimer.Enabled = false;
                inventoryClient.DisconnectInventory();
            }
        }

        [RestResource]
        public class NETCONFBrokerResource
        {
            [RestRoute(HttpMethod = HttpMethod.GET, PathInfo = "/Inventory")]
            public IHttpContext GET_Inventory(IHttpContext context)
            {
                HttpListenerBasicIdentity identity = (HttpListenerBasicIdentity)context.User.Identity;

                if (IsAuthorized(identity, 'r'))
                {
                    context.Response.ContentType = ContentType.JSON;
                    context.Response.SendResponse(JsonConvert.SerializeObject(inventoryClient.Nodes));
                    return context;
                }
                else
                {
                    context.Response.StatusCode = Grapevine.Shared.HttpStatusCode.Unauthorized;
                }
                return context;
            }

            [RestRoute]
            public IHttpContext HelloAPI(IHttpContext context)
            {
                HttpListenerBasicIdentity identity = (HttpListenerBasicIdentity)context.User.Identity;

                if (IsAuthorized(identity, 'r'))
                {
                    context.Response.ContentType = ContentType.TEXT;
                    context.Response.SendResponse("NETCONF REST Broker");
                    return context;
                }
                else
                {
                    context.Response.StatusCode = Grapevine.Shared.HttpStatusCode.Unauthorized;
                }
                return context;
            }

            static bool IsAuthorized(HttpListenerBasicIdentity Identity, char Permission)
            {
                switch (Permission)
                {
                    case 'r':
                        if (config.ServerBasicAuth.ReadOnly.Username == Identity.Name && config.ServerBasicAuth.ReadOnly.Password == Identity.Password ||
                            config.ServerBasicAuth.ReadWrite.Username == Identity.Name && config.ServerBasicAuth.ReadWrite.Password == Identity.Password)
                        {
                            return true;
                        }
                        break;
                    case 'w':
                        if (config.ServerBasicAuth.ReadWrite.Username == Identity.Name && config.ServerBasicAuth.ReadWrite.Password == Identity.Password)
                        {
                            return true;
                        }
                        break;
                    default:
                        return false;
                }

                return false;
            }
        }
    }
}
