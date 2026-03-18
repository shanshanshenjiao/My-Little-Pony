using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace Sanguosha.Server
{
    /// <summary>
    /// TCP 服务器 - 局域网游戏服务器
    /// </summary>
    public class TCPServer : MonoBehaviour
    {
        public static TCPServer Instance { get; private set; }
        
        [Header("服务器配置")]
        public int port = 8888;
        public int maxClients = 4;
        
        private TcpListener server;
        private Dictionary<string, ClientData> clients;
        private bool isRunning;
        private Thread acceptThread;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
            
            clients = new Dictionary<string, ClientData>();
        }
        
        /// <summary>
        /// 启动服务器
        /// </summary>
        public void StartServer()
        {
            try
            {
                server = new TcpListener(IPAddress.Any, port);
                server.Start();
                isRunning = true;
                
                Debug.Log($"[服务器] 已启动于端口 {port}");
                
                // 启动接受连接线程
                acceptThread = new Thread(AcceptConnections);
                acceptThread.IsBackground = true;
                acceptThread.Start();
                
            }
            catch (Exception e)
            {
                Debug.LogError($"[服务器] 启动失败：{e.Message}");
            }
        }
        
        /// <summary>
        /// 接受客户端连接
        /// </summary>
        private void AcceptConnections()
        {
            while (isRunning)
            {
                try
                {
                    if (!server.Pending())
                    {
                        Thread.Sleep(100);
                        continue;
                    }
                    
                    TcpClient client = server.AcceptTcpClient();
                    string clientId = $"client_{clients.Count + 1}";
                    
                    Debug.Log($"[服务器] 新连接：{client.Client.RemoteEndPoint}");
                    
                    // 创建客户端数据
                    ClientData clientData = new ClientData
                    {
                        id = clientId,
                        client = client,
                        stream = client.GetStream(),
                        connectedAt = DateTime.Now
                    };
                    
                    clients.Add(clientId, clientData);
                    
                    // 启动接收消息线程
                    Thread receiveThread = new Thread(() => ReceiveMessages(clientData));
                    receiveThread.IsBackground = true;
                    receiveThread.Start();
                    
                    // 发送欢迎消息
                    SendToClient(clientId, new Network.NetworkMessage
                    {
                        type = Network.MessageType.Welcome,
                        payload = $"{{\"playerId\":\"{clientId}\"}}"
                    });
                    
                    if (clients.Count >= maxClients)
                    {
                        Debug.Log("[服务器] 玩家数量已达上限");
                        break;
                    }
                    
                }
                catch (Exception e)
                {
                    if (isRunning)
                    {
                        Debug.LogError($"[服务器] 接受连接错误：{e.Message}");
                    }
                }
            }
        }
        
        /// <summary>
        /// 接收客户端消息
        /// </summary>
        private void ReceiveMessages(ClientData clientData)
        {
            byte[] buffer = new byte[4096];
            
            try
            {
                while (isRunning && clientData.client.Connected)
                {
                    int bytesRead = clientData.stream.Read(buffer, 0, buffer.Length);
                    
                    if (bytesRead <= 0)
                    {
                        break;
                    }
                    
                    string json = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Debug.Log($"[服务器] 收到消息：{json}");
                    
                    // 处理消息
                    HandleMessage(clientData.id, json);
                }
            }
            catch (Exception e)
            {
                Debug.Log($"[服务器] 客户端 {clientData.id} 断开：{e.Message}");
            }
            finally
            {
                RemoveClient(clientData.id);
            }
        }
        
        /// <summary>
        /// 处理客户端消息
        /// </summary>
        private void HandleMessage(string clientId, string json)
        {
            try
            {
                Network.NetworkMessage message = JsonUtility.FromJson<Network.NetworkMessage>(json);
                
                switch (message.type)
                {
                    case Network.MessageType.JoinGame:
                        HandleJoinGame(clientId, message.payload);
                        break;
                        
                    case Network.MessageType.CardPlayed:
                        // 转发给其他客户端
                        Broadcast(message, clientId);
                        break;
                        
                    case Network.MessageType.EndTurn:
                        // 转发给其他客户端
                        Broadcast(message, clientId);
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[服务器] 消息处理错误：{e.Message}");
            }
        }
        
        private void HandleJoinGame(string clientId, string payload)
        {
            Debug.Log($"[服务器] 玩家 {clientId} 加入游戏");
            
            // 广播玩家加入消息
            Network.NetworkMessage msg = new Network.NetworkMessage
            {
                type = Network.MessageType.JoinGame,
                payload = payload
            };
            Broadcast(msg);
        }
        
        /// <summary>
        /// 发送消息到指定客户端
        /// </summary>
        public void SendToClient(string clientId, Network.NetworkMessage message)
        {
            if (!clients.ContainsKey(clientId))
            {
                Debug.LogWarning($"[服务器] 客户端 {clientId} 不存在");
                return;
            }
            
            try
            {
                ClientData clientData = clients[clientId];
                string json = JsonUtility.ToJson(message);
                byte[] data = System.Text.Encoding.UTF8.GetBytes(json);
                
                clientData.stream.Write(data, 0, data.Length);
                clientData.stream.Flush();
            }
            catch (Exception e)
            {
                Debug.LogError($"[服务器] 发送消息失败：{e.Message}");
            }
        }
        
        /// <summary>
        /// 广播消息到所有客户端
        /// </summary>
        public void Broadcast(Network.NetworkMessage message, string excludeId = null)
        {
            foreach (var client in clients.Values)
            {
                if (client.id != excludeId)
                {
                    SendToClient(client.id, message);
                }
            }
        }
        
        /// <summary>
        /// 移除客户端
        /// </summary>
        private void RemoveClient(string clientId)
        {
            if (clients.ContainsKey(clientId))
            {
                ClientData clientData = clients[clientId];
                
                try
                {
                    clientData.client.Close();
                }
                catch { }
                
                clients.Remove(clientId);
                Debug.Log($"[服务器] 客户端 {clientId} 已移除");
                
                // 广播玩家离开消息
                Network.NetworkMessage msg = new Network.NetworkMessage
                {
                    type = Network.MessageType.JoinGame, // TODO: 使用 LeaveGame 类型
                    payload = $"{{\"playerId\":\"{clientId}\"}}"
                };
                Broadcast(msg);
            }
        }
        
        /// <summary>
        /// 停止服务器
        /// </summary>
        public void StopServer()
        {
            isRunning = false;
            
            if (server != null)
            {
                server.Stop();
            }
            
            foreach (var client in clients.Values)
            {
                try
                {
                    client.client.Close();
                }
                catch { }
            }
            
            clients.Clear();
            Debug.Log("[服务器] 已关闭");
        }
        
        private void OnDestroy()
        {
            StopServer();
        }
    }
    
    /// <summary>
    /// 客户端数据结构
    /// </summary>
    public class ClientData
    {
        public string id;
        public TcpClient client;
        public NetworkStream stream;
        public DateTime connectedAt;
    }
}
