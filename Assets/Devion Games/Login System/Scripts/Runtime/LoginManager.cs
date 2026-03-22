using DevionGames.LoginSystem.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace DevionGames.LoginSystem
{
    public class LoginManager : MonoBehaviour
    {
        private static LoginManager m_Current;

        public static LoginManager current
        {
            get
            {
                Assert.IsNotNull(m_Current, "Requires Login Manager.Create one from Tools > Devion Games > Login System > Create Login Manager!");
                return m_Current;
            }
        }

        private void Awake()
        {
            if (LoginManager.m_Current != null)
            {
                if (LoginManager.DefaultSettings.debug)
                    Debug.Log("Multiple LoginManager in scene...this is not supported. Destroying instance!");
                Destroy(gameObject);
                return;
            }
            else
            {
                LoginManager.m_Current = this;
                if (LoginManager.DefaultSettings.debug)
                    Debug.Log("LoginManager initialized.");
            }
        }

        private void Start()
        {
            // ❗ 保持原逻辑：如果跳过登录就进游戏
            if (LoginManager.DefaultSettings.skipLogin)
            {
                if (LoginManager.DefaultSettings.debug)
                    Debug.Log("Login System is disabled...Loading " + LoginManager.DefaultSettings.sceneToLoad + " scene.");
                UnityEngine.SceneManagement.SceneManager.LoadScene("SampleScene");
            }
            else
            {
                // ✅ 确保登录界面打开（关键）
                EventHandler.Execute("OnLogout");
            }
        }

        [SerializeField]
        private LoginConfigurations m_Configurations = null;

        public static LoginConfigurations Configurations
        {
            get
            {
                if (LoginManager.current != null)
                {
                    Assert.IsNotNull(LoginManager.current.m_Configurations, "Please assign Login Configurations to the Login Manager!");
                    return LoginManager.current.m_Configurations;
                }
                return null;
            }
        }

        private static Default m_DefaultSettings;
        public static Default DefaultSettings
        {
            get
            {
                if (m_DefaultSettings == null)
                {
                    m_DefaultSettings = GetSetting<Default>();
                }
                return m_DefaultSettings;
            }
        }

        private static UI m_UI;
        public static UI UI
        {
            get
            {
                if (m_UI == null)
                {
                    m_UI = GetSetting<UI>();
                }
                return m_UI;
            }
        }

        private static Notifications m_Notifications;
        public static Notifications Notifications
        {
            get
            {
                if (m_Notifications == null)
                {
                    m_Notifications = GetSetting<Notifications>();
                }
                return m_Notifications;
            }
        }

        private static Server m_Server;
        public static Server Server
        {
            get
            {
                if (m_Server == null)
                {
                    m_Server = GetSetting<Server>();
                }
                return m_Server;
            }
        }

        private static T GetSetting<T>() where T : Configuration.Settings
        {
            if (LoginManager.Configurations != null)
            {
                return (T)LoginManager.Configurations.settings.Where(x => x.GetType() == typeof(T)).FirstOrDefault();
            }
            return default(T);
        }

        // ===========================
        // 🟢 本地注册
        // ===========================
        public static void CreateAccount(string username, string password, string email)
        {
            if (LoginManager.current != null)
            {
                LoginManager.current.StartCoroutine(CreateAccountInternal(username, password, email));
            }
        }

        private static IEnumerator CreateAccountInternal(string username, string password, string email)
        {
            if (LoginManager.DefaultSettings.debug)
                Debug.Log("[CreateAccount] 本地注册: " + username);

            string key = "USER_" + username;

            if (PlayerPrefs.HasKey(key))
            {
                Debug.Log("[CreateAccount] ❌ 用户已存在");
                EventHandler.Execute("OnFailedToCreateAccount");
                yield break;
            }

            PlayerPrefs.SetString(key, password);
            PlayerPrefs.SetString("EMAIL_" + username, email);
            PlayerPrefs.Save();

            Debug.Log("[CreateAccount] ✅ 注册成功（本地）");

            EventHandler.Execute("OnAccountCreated");
        }

        // ===========================
        // 🟢 本地登录
        // ===========================
        public static void LoginAccount(string username, string password)
        {
            if (LoginManager.current != null)
            {
                LoginManager.current.StartCoroutine(LoginAccountInternal(username, password));
            }
        }

        private static IEnumerator LoginAccountInternal(string username, string password)
        {
            if (LoginManager.DefaultSettings.debug)
                Debug.Log("[LoginAccount] 本地登录: " + username);

            string key = "USER_" + username;

            if (!PlayerPrefs.HasKey(key))
            {
                Debug.Log("[LoginAccount] ❌ 用户不存在");
                EventHandler.Execute("OnFailedToLogin");
                yield break;
            }

            string savedPassword = PlayerPrefs.GetString(key);

            if (savedPassword == password)
            {
                PlayerPrefs.SetString(LoginManager.Server.accountKey, username);

                Debug.Log("[LoginAccount] ✅ 登录成功（本地）");

                EventHandler.Execute("OnLogin");
            }
            else
            {
                Debug.Log("[LoginAccount] ❌ 密码错误");
                EventHandler.Execute("OnFailedToLogin");
            }
        }

        // ===========================
        // ❌ 以下功能暂时禁用（本地模式）
        // ===========================

        public static void RecoverPassword(string email)
        {
            Debug.Log("本地模式不支持找回密码");
            EventHandler.Execute("OnFailedToRecoverPassword");
        }

        public static void ResetPassword(string username, string password)
        {
            Debug.Log("本地模式不支持重置密码");
            EventHandler.Execute("OnFailedToResetPassword");
        }

        // ===========================
        // 📧 邮箱验证（保留）
        // ===========================
        public static bool ValidateEmail(string email)
        {
            System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$");
            System.Text.RegularExpressions.Match match = regex.Match(email);

            if (match.Success)
            {
                if (LoginManager.DefaultSettings.debug)
                    Debug.Log("Email validation was successfull for email: " + email + "!");
            }
            else
            {
                if (LoginManager.DefaultSettings.debug)
                    Debug.Log("Email validation failed for email: " + email + "!");
            }

            return match.Success;
        }
    }
}