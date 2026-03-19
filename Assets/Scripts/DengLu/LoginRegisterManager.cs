using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoginRegisterManager : MonoBehaviour
{
    [Header("UI组件")]
    public InputField registerUsernameInput; // 注册账号输入框
    public InputField registerPasswordInput; // 注册密码输入框
    public InputField loginUsernameInput;    // 登录账号输入框
    public InputField loginPasswordInput;    // 登录密码输入框
    public Text tipText;                     // 提示文本（显示成功/失败信息）

    // 存储用户数据的Key（PlayerPrefs的唯一标识）
    private const string USER_DATA_KEY = "All_User_Data";

    // 注册按钮点击事件（绑定到UI按钮）
    public void OnRegisterButtonClick()
    {
        // 1. 获取输入内容并去空格
        string username = registerUsernameInput.text.Trim();
        string password = registerPasswordInput.text.Trim();

        // 2. 输入校验
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ShowTip("账号或密码不能为空！");
            return;
        }

        // 3. 读取本地所有用户数据
        UserDataList userList = LoadAllUserData();

        // 4. 检查账号是否已存在
        if (IsUsernameExist(userList, username))
        {
            ShowTip("该账号已注册！");
            return;
        }

        // 5. 添加新用户并保存
        userList.users.Add(new UserData
        {
            username = username,
            password = password
        });
        SaveAllUserData(userList);

        // 6. 提示并清空输入框
        ShowTip("注册成功！");
        ClearRegisterInput();
    }

    // 登录按钮点击事件（绑定到UI按钮）
    public void OnLoginButtonClick()
    {
        // 1. 获取输入内容并去空格
        string username = loginUsernameInput.text.Trim();
        string password = loginPasswordInput.text.Trim();

        // 2. 输入校验
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ShowTip("账号或密码不能为空！");
            return;
        }

        // 3. 读取本地所有用户数据
        UserDataList userList = LoadAllUserData();

        // 4. 校验账号密码
        UserData matchUser = userList.users.Find(u => u.username == username && u.password == password);
        if (matchUser != null)
        {
            ShowTip("登录成功！");
            ClearLoginInput();
            // 登录成功后的逻辑（如跳转到主界面）
            OnLoginSuccess();
        }
        else
        {
            ShowTip("账号或密码错误！");
        }
    }

    // 读取本地所有用户数据
    private UserDataList LoadAllUserData()
    {
        UserDataList userList = new UserDataList();
        // 从PlayerPrefs读取JSON字符串
        string json = PlayerPrefs.GetString(USER_DATA_KEY, "");
        if (!string.IsNullOrEmpty(json))
        {
            // JSON反序列化为用户列表
            userList = JsonUtility.FromJson<UserDataList>(json);
        }
        return userList;
    }

    // 保存所有用户数据到本地
    private void SaveAllUserData(UserDataList userList)
    {
        // 将用户列表序列化为JSON字符串
        string json = JsonUtility.ToJson(userList);
        // 存储到PlayerPrefs（持久化，重启Unity也不会丢失）
        PlayerPrefs.SetString(USER_DATA_KEY, json);
        PlayerPrefs.Save(); // 强制保存，避免数据丢失
    }

    // 检查账号是否已存在
    private bool IsUsernameExist(UserDataList userList, string username)
    {
        return userList.users.Exists(u => u.username == username);
    }

    // 显示提示信息
    private void ShowTip(string content)
    {
        tipText.text = content;
        // 3秒后清空提示（可选）
        Invoke("ClearTip", 3f);
    }

    // 清空注册输入框
    private void ClearRegisterInput()
    {
        registerUsernameInput.text = "";
        registerPasswordInput.text = "";
    }

    // 清空登录输入框
    private void ClearLoginInput()
    {
        loginUsernameInput.text = "";
        loginPasswordInput.text = "";
    }

    // 清空提示文本
    private void ClearTip()
    {
        tipText.text = "";
    }

    // 登录成功后的逻辑（可自定义，如跳转场景）
    private void OnLoginSuccess()
    {
        Debug.Log("登录成功，跳转到主界面！");
        // SceneManager.LoadScene("MainScene"); // 需引入using UnityEngine.SceneManagement;
    }

    // 可选：注销/清除所有用户数据（测试用）
    public void OnClearAllUserData()
    {
        PlayerPrefs.DeleteKey(USER_DATA_KEY);
        PlayerPrefs.Save();
        ShowTip("所有用户数据已清除！");
    }
}