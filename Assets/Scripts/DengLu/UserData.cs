using System;
using System.Collections.Generic;
using UnityEngine;

// 用户信息类（需序列化才能转JSON）
[Serializable]
public class UserData
{
    public string username; // 账号
    public string password; // 密码（注：示例仅明文存储，实际项目需加密）
}

// 存储所有用户的容器（需序列化）
[Serializable]
public class UserDataList
{
    public List<UserData> users = new List<UserData>();
}