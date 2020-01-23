﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Server
{
    class ChatServer
    {
        /// <summary>
        /// Maximum number of concurrent connections on the server
        /// </summary>
        private static readonly int _maxClients = 10;

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
        private readonly ConcurrentQueue<Tuple<string,string>> _tosend = new ConcurrentQueue<Tuple<string, string>>();

        /// <summary>
        /// Lock used to block the access to the clients list
        /// </summary>
        private readonly object _lock = new object();

        /// <summary>
        /// The Task that listens to the incoming connections and accept them if the room
        /// is not full
        /// </summary>
        private Task _listenTask;
 
        /// <summary>
        /// The Task being used to dispatch the waiting messages to the connected clientss
        /// </summary>
        private Task _dispatchTask;

        /// <summary>
        /// Create a new Chat server on the desired IP address and port
        /// </summary>
        /// <param name="ip">IP Address to listen to</param>
        /// <param name="port">Port to listen to</param>
        public ChatServer(string ip, int port)
        {
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
            // As long as there are messages we dispatch them
            while (_tosend.Count > 0)
            {
                // Get the first message of the list and send it to all clients
                _tosend.TryDequeue(out Tuple<string,string> msg);

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

            // Add the message to the message queue and if the server is not dispatching 
            // make it dispatch again
            _tosend.Enqueue(new Tuple<string, string>(user,msg));
            if (_dispatchTask.IsCompleted)
            {
                _dispatchTask = new Task(DistpatchMessages);
                _dispatchTask.Start();
            }
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