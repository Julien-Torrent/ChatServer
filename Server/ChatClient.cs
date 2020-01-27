using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    /// <summary>
    /// A class that represents a ChatClient, a person connected to the server.
    /// A Chat client has a Username and can receive and send messages
    /// </summary>
    class ChatClient
    {
        /// <summary>
        /// The action to execute when the ChatClient recieves a message
        /// The first string is the Username of this ChatClient
        /// The second string is the message that has been recieved
        /// </summary>
        public Action<string, string> OnMessageRecived { get; set; }

        /// <summary>
        /// The action to execute when the ChatClient is disconnected from the server
        /// The given parameter will always be the ChatClient that is disconnected
        /// </summary>
        public Action<ChatClient> OnClientDisconnected { get; set; }

        /// <summary>
        /// The Username of this ChatClient, should be sent dirrectly (Within 50ms) after opening the TcpConnection.
        /// The username will be the first thing read during the 50ms of the opening of the TcpConnection
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// The TcpClient the ChatClient is using
        /// </summary>
        private readonly TcpClient _client;

        /// <summary>
        /// Create a new ChatClient, with the desired opened TcpClient. The Chat client can send messages and
        /// notify when it is disconnected from the server or when it recieves a new message
        /// </summary>
        /// <param name="client">the TcpClient that the client will be using to send and receive data</param>
        /// <param name="onMessageRecived">The action to execute when the TcpClient reads a message from the Stream</param>
        /// <param name="onClientDisconnected">The action to execute when the TcpClient is disconnected from the server</param>
        public ChatClient(TcpClient client, Action<string,string> onMessageRecived, Action<ChatClient> onClientDisconnected)
        {
            _client = client;

            OnMessageRecived = onMessageRecived;
            OnClientDisconnected = onClientDisconnected;

            // Wait 50ms, before trying to recover the username
            Task.Delay(50).Wait();
            UserName = GetDataAsync().Result;

            // Trigger the OnMessageRecived with a predifined message
            OnMessageRecived("System", $"{UserName} has connected");

            // Start the Read in a new Task
            Task.Run(() => Read());
        }   

        /// <summary>
        /// Try to write the message the client, if it fails triggers the OnClientDisconnected
        /// </summary>
        /// <param name="message">the UTF8 message to send to this client</param>
        /// <returns>An awaitable Task</returns>
        public async Task WriteAsync(string message)
        {
            try
            {
                await _client.GetStream().WriteAsync(Encoding.UTF8.GetBytes(message));
            }
            catch (IOException)
            {
                // Trigger the disconnected action
                OnClientDisconnected(this);
            }
        }

        /// <summary>
        /// Closes the socket and the TcpClient
        /// </summary>
        public void Close()
        {
            // When the client has disconnected, dispose the socket and tcpclient
            _client.Client.Close();
            _client.Close();
        }

        /// <summary>
        /// Read the data from the TcpClient Stream as a UTF8 string.
        /// </summary>
        /// <returns>A UTF8 string read from the stream</returns>
        private async Task<string> GetDataAsync()
        {
            byte[] buffer = new byte[_client.Available];
            int bytes = await _client.GetStream().ReadAsync(buffer, 0, _client.Available);
            return Encoding.UTF8.GetString(buffer, 0, bytes);
        }

        /// <summary>
        /// Continuously read the data from the TcpClient, until the TcpClient is disconnected
        /// </summary>
        /// <returns>Awaitable Task</returns>
        private async Task Read()
        {
            // While the client is connected, keep looking on the data stream
            while(_client.Connected)
            {
                try
                {
                    // Method to update the TcpClient.Connected property, try to send empty data
                    await _client.GetStream().WriteAsync(Array.Empty<byte>());
                }
                catch (IOException)
                {
                    // Trigger the disconnected action and break the loop
                    OnClientDisconnected(this);
                    break;
                }

                // As long as there is data, read and send to OnMessageRecived with the username
                while (_client.GetStream().DataAvailable)
                {
                    OnMessageRecived(UserName, await GetDataAsync());
                }

                // Wait for 50ms, to prevent too much CPU usage
                await Task.Delay(50);
            }

            // Close the underling connection
            Close();
        }
    }
}
