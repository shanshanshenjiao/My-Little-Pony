using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using System.Threading.Tasks;

public class LoginController : MonoBehaviour
{
    void Start()
    {
        UIManager.Instance.startButton.onClick.AddListener(() =>
        {
            OnClickLogin();
        });
    }

    public async void OnClickLogin()
    {
        try
        {
            Debug.Log("点击开始按钮");

            // ? 防止重复初始化
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
                Debug.Log("服务初始化完成");
            }

            // ? 防止重复登录
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log("登录成功");
            }
            else
            {
                Debug.Log("已经登录过了");
            }

            // ?? 切UI
            UIManager.Instance.loginPanel.SetActive(false);
            UIManager.Instance.mainPanel.SetActive(true);
        }
        catch (System.Exception e)
        {
            Debug.LogError("登录失败：" + e.Message);
        }
    }
}