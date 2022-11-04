using System.Net;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

internal class MainMenuFunctions : MonoBehaviour
{
    public InputField username = null;
    public InputField password = null;
    public InputField ipAddress = null;
    public Text errorText = null;
    public Toggle saveUsername = null;
    public Toggle saveIPAddress = null;

    private static string ErrorMessageForSceneReload = "";
    internal static MainMenuFunctions mmf;

    /// <summary>
    /// Sets variables and reads last used IP and password. If an error message was set, displays it
    /// </summary>
    void Start()
    {
        mmf = this;

        ipAddress.text = "127.0.0.1";
        if (File.Exists(@"Username.txt"))
        {
            using (StreamReader savefile = new StreamReader(@"Username.txt"))
            {
                username.text = savefile.ReadLine();
            }
        }
        if (File.Exists(@"IPAddress.txt"))
        {
            using (StreamReader savefile = new StreamReader(@"IPAddress.txt"))
            {
                ipAddress.text = savefile.ReadLine();
            }
        }

        DisplayErrorMessage();
    }

    /// <summary>
    /// Update is called once per frame
    /// </summary>
    void Update() { }

    /// <summary>
    /// Sets error message for user
    /// </summary>
    internal static void SetErrorMessage(string error)
    {
        ErrorMessageForSceneReload = error;
    }

    /// <summary>
    /// Displays any previously set error message
    /// </summary>
    private void DisplayErrorMessage()
    {
        errorText.text = ErrorMessageForSceneReload;
        SetErrorMessage("");
    }

    /// <summary>
    /// Checks valid data and attempts to connect client
    /// </summary>
    internal void Button_Connect()
    {
        string errorMessage = "";
        bool canLogin = true;
        IPAddress address = null;

        // Verify
        if (string.IsNullOrEmpty(username.text))
        {
            errorMessage += "Missing Username!\n";
            canLogin = false;
        }
#if !UNITY_EDITOR
        canLogin = canLogin & CheckPassword(ref errorMessage);
#endif
        if (string.IsNullOrEmpty(ipAddress.text))
        {
            errorMessage += "Missing IPAddress!\n";
            canLogin = false;
        }
        else if (!IPAddress.TryParse(ipAddress.text, out address))
        {
            errorMessage += "Invalid IPAddress!\n";
            canLogin = false;
        }

        // Log-in valid
        if (!canLogin)
        {
            ErrorMessageForSceneReload = errorMessage;
            DisplayErrorMessage();
            return;
        }

        // Save information
        if (saveUsername.isOn)
        {
            using (StreamWriter savefile = new StreamWriter(@"Username.txt"))
            {
                savefile.WriteLine(username.text);
            }
        }
        if (saveIPAddress.isOn)
        {
            using (StreamWriter savefile = new StreamWriter(@"IPAddress.txt"))
            {
                savefile.WriteLine(ipAddress.text);
            }
        }

        // Try and connect
        ClientGameLoop.CGL.ConnectToMasterServer(address, username.text, password.text);
    }

    /// <summary>
    /// Checks password, shows error if empty
    /// </summary>
    /// <param name="errorMessage"></param>
    /// <param name="canLogin"></param>
    private bool CheckPassword(ref string errorMessage)
    {
        if (string.IsNullOrEmpty(password.text))
        {
            errorMessage += "Missing Password!\n";
            return false;
        }
        return true;
    }

    /// <summary>
    /// Shutsdown
    /// </summary>
    internal void Button_Exit()
    {
        Application.Quit();
    }
}
