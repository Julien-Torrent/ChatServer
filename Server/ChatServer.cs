using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    class ChatServer
    {
        /// <summary>
        /// Maximum number of concurrent connections on the server
        /// </summary>
        private readonly int _maxClients;

        /// <summary>
        /// The TcpListener to accept the incoming connections when the server is not full
        /// </summary>
        private readonly TcpListener _listener;

        /// <summary>
        /// The list of the ChatClients currently connected to the server
        /// </summary>
        private readonly List<ChatClient> _clients = new List<ChatClient>();

        /// <summary>
        /// List of the messages the server needs to dispatch to the connected clients
        /// </summary>
        private readonly BlockingCollection<Tuple<string, string>> _tosend = new BlockingCollection<Tuple<string, string>>();

        /// <summary>
        /// The CancellationTokenSource used when the server stop is requested 
        /// </summary>
        private readonly CancellationTokenSource _cancellationToken = new CancellationTokenSource();

        /// <summary>
        /// Lock used to block the access to the clients list
        /// </summary>
        private readonly object _lock = new object();

        /// <summary>
        /// The Task that listens to the incoming connections and accept them if the room is not full
        /// </summary>
        private Task _listenTask;
 
        /// <summary>
        /// The Task being used to dispatch the waiting messages to the connected clients
        /// </summary>
        private readonly Task _dispatchTask;

        /// <summary>
        /// Create a new Chat server on the desired IP address and port
        /// </summary>
        /// <param name="ip">IP Address to listen to</param>
        /// <param name="port">Port to listen to</param>
        public ChatServer(string ip, int port, int maxClients)
        {
            _maxClients = maxClients;
            _listener = new TcpListener(new IPEndPoint(IPAddress.Parse(ip), port));

            _listenTask = new Task(Listen);
            _dispatchTask = new Task(DistpatchMessages);
        }

        /// <summary>
        /// Start the ChatServer, it accepts the incoming connections and start 
        /// to recieve and dispatch the incoming messages
        /// </summary>
        public void Start()
        {
            // Start to accept the connection and dispatch the messages
            _listenTask.Start();
            _dispatchTask.Start();
        }

        /// <summary>
        /// Stops the server. Closes all the ChatClients
        /// </summary>
        public void Stop()
        {
            lock (_lock)
            {
                // Closes all the ChatClients
                foreach (var c in _clients)
                {
                    c.Close();
                }
            }

            // Cancel the tasks
            _cancellationToken.Cancel();
        }

        /// <summary>
        /// Method to accept the incoming connections and create a new ChatClient 
        /// and add it to the list of the conncted clients
        /// </summary>
        private void Listen()
        {
            _listener.Start();

            // While the room is not full accept connections
            while (_clients.Count < _maxClients)
            {
                // Accept the connection
                var client = _listener.AcceptTcpClient();

                lock (_lock)
                {
                    // Add to the list of the connected clients
                    _clients.Add(new ChatClient(client, Recieve, Disconnect));
                }
            }

            _listener.Stop();
        }


        /// <summary>
        /// Dispatch the messages in the queue to the connected clients
        /// </summary>
        private void DistpatchMessages()
        {
            // Send each message in the queue to the connected clients except to the sender of the message
            foreach (var msg in _tosend.GetConsumingEnumerable(_cancellationToken.Token))
            {
                lock (_lock)
                {
                    foreach (var c in _clients.Where(x => x.UserName != msg.Item1))
                    {
                        c.WriteAsync(msg.Item1 + ": " + msg.Item2).Wait();
                    }
                }
            }
        }

        /// <summary>
        /// Method called when a client recieves a message
        /// </summary>
        /// <param name="user">Name of the user (ChatClient) that has recieved a message</param>
        /// <param name="msg">Message the server has recieved</param>
        private void Recieve(string user, string msg)
        {
            // Log on the server
            Console.WriteLine($"[{user}] {msg}");

            // Add the message to the message queue
            _tosend.Add(new Tuple<string, string>(user,msg));
        }

        /// <summary>
        /// Method called when a client has disconnected from the server
        /// </summary>
        /// <param name="sender">The client that has just disconnected</param>
        private void Disconnect(ChatClient sender)
        {
            // Triggers a message that the client has disconnected
            sender.OnMessageRecived("System", $"{sender.UserName} has disconnected");

            lock (_lock)
            {
                // Removes it from the connected clients list
                _clients.Remove(sender);
            }

            // If the server was full (task is finished), we start lisening again
            if (_listenTask.IsCompleted)
            {
                _listenTask = new Task(Listen);
                _listenTask.Start();
            }
        }
    }
}
