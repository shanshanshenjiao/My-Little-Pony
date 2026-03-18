using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sanguosha.Network
{
    /// <summary>
    /// 网络管理器 - 处理局域网通信
    /// </summary>
    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance { get; private set; }
        
        [Header("网络配置")]
        public string serverAddress = "127.0.0.1";
        public int serverPort = 8888;
        public bool isServer;
        
        [Header("连接状态")]
        public bool isConnected;
        public string localPlayerId;
        
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
        }
        
        /// <summary>
        /// 作为服务器启动
        /// </summary>
        public void StartServer()
        {
            isServer = true;
            Debug.Log($"服务器启动于 {serverAddress}:{serverPort}");
            // TODO: 实现 Socket 服务器逻辑
        }
        
        /// <summary>
        /// 连接到服务器
        /// </summary>
        public void ConnectToServer(string address, int port)
        {
            serverAddress = address;
            serverPort = port;
            
            Debug.Log($"正在连接到 {address}:{port}...");
            StartCoroutine(ConnectCoroutine());
        }
        
        private IEnumerator ConnectCoroutine()
        {
            // TODO: 实现 TCP/WebSocket 连接
            yield return new WaitForSeconds(1f);
            
            isConnected = true;
            localPlayerId = $"player_{UnityEngine.Random.Range(1000, 9999)}";
            
            Debug.Log($"连接成功！玩家 ID: {localPlayerId}");
            
            // 发送加入游戏消息
            SendMessage(new NetworkMessage
            {
                type = MessageType.JoinGame,
                payload = JsonUtility.ToJson(new JoinGamePayload
                {
                    playerId = localPlayerId,
                    username = PlayerPrefs.GetString("Username", "Player")
                })
            });
        }
        
        /// <summary>
        /// 发送消息
        /// </summary>
        public void SendMessage(NetworkMessage message)
        {
            if (!isConnected)
            {
                Debug.LogWarning("未连接到服务器");
                return;
            }
            
            string json = JsonUtility.ToJson(message);
            Debug.Log($"[发送] {json}");
            
            // TODO: 实现实际的网络发送
        }
        
        /// <summary>
        /// 接收消息（由网络线程调用）
        /// </summary>
        public void OnReceiveMessage(string json)
        {
            try
            {
                NetworkMessage message = JsonUtility.FromJson<NetworkMessage>(json);
                HandleMessage(message);
            }
            catch (Exception e)
            {
                Debug.LogError($"消息解析失败：{e.Message}");
            }
        }
        
        /// <summary>
        /// 处理接收到的消息
        /// </summary>
        private void HandleMessage(NetworkMessage message)
        {
            Debug.Log($"[接收] 类型：{message.type}");
            
            switch (message.type)
            {
                case MessageType.Welcome:
                    HandleWelcome(message.payload);
                    break;
                    
                case MessageType.GameStart:
                    HandleGameStart(message.payload);
                    break;
                    
                case MessageType.CardPlayed:
                    HandleCardPlayed(message.payload);
                    break;
                    
                case MessageType.TurnEnded:
                    HandleTurnEnded(message.payload);
                    break;
                    
                case MessageType.GameOver:
                    HandleGameOver(message.payload);
                    break;
            }
        }
        
        private void HandleWelcome(string payload)
        {
            Debug.Log("收到欢迎消息");
        }
        
        private void HandleGameStart(string payload)
        {
            Debug.Log("游戏开始！");
            // 触发游戏开始 UI
        }
        
        private void HandleCardPlayed(string payload)
        {
            Debug.Log("有玩家出牌");
        }
        
        private void HandleTurnEnded(string payload)
        {
            Debug.Log("回合结束");
        }
        
        private void HandleGameOver(string payload)
        {
            Debug.Log("游戏结束");
        }
        
        /// <summary>
        /// 断开连接
        /// </summary>
        public void Disconnect()
        {
            isConnected = false;
            Debug.Log("已断开连接");
        }
    }
    
    /// <summary>
    /// 网络消息数据结构
    /// </summary>
    [Serializable]
    public class NetworkMessage
    {
        public MessageType type;
        public string payload;
        public long timestamp;
        
        public NetworkMessage()
        {
            timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
        }
    }
    
    /// <summary>
    /// 消息类型枚举
    /// </summary>
    public enum MessageType
    {
        None,
        Welcome,
        JoinGame,
        GameStart,
        CardPlayed,
        TurnEnded,
        GameOver,
        Error,
        EndTurn
    }
    
    /// <summary>
    /// 加入游戏载荷
    /// </summary>
    [Serializable]
    public class JoinGamePayload
    {
        public string playerId;
        public string username;
    }
}
