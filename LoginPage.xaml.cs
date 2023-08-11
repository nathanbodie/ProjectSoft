using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Security;
using System.Security.Cryptography;
using static ProjectSoft.DatabaseAccess;
using System.Data;
using System.Threading;

namespace ProjectSoft
{
    /// <summary>
    /// Interaction logic for LoginPage.xaml
    /// </summary>
    public partial class LoginPage : Page
    {
        Frame frame;
        public LoginPage(Frame frame)
        {
            this.frame = frame;
            InitializeComponent();
        }

        private void Login_Submit_btn_Click(object sender, RoutedEventArgs e)
        {
            Message_txt.Text = "";
            Debug.WriteLine("Attempting to log in...");
            if(username_txt.Text == "" || user_password_txt.Password == "")
            {   // Empty username and/or password
                InvalidCredentials("Username or password is null.");
                return;
            }
            string userUID = AuthenticateUser(username_txt.Text, user_password_txt.Password);
            if (userUID != "")
            {   //  Valid credentials - Log User in.
                Debug.WriteLine("Authentication Successful!");
                frame.Navigate(new MainMenu(frame, userUID));
            }
            else
            {   //  Invalid credentials
                return;
            }
        }

        private string AuthenticateUser(string username, string password)
        {
            DataTable dt = Select("SELECT [UID] FROM [dbo].[USERS] WHERE [USERNAME] = '" + username + "'");
            if (dt.Rows.Count == 0)
            {   // Invalid Username
                InvalidCredentials("Invalid Username.");
                return "";
            }
            string? userUID = dt.Rows[0].Field<string>("UID");
            if (userUID == null) { return ""; }
            dt = Select("SELECT [PASSWORD] FROM [dbo].[CREDENTIALS] WHERE [USER_UID] = '" + userUID + "'");
            if(dt.Rows.Count == 0)
            {   // Unavailable Password... (should never occur)
                InvalidCredentials("Unavailable Password.");
                return "";
            }
            string hashedPassword;
            using (SHA512 sha = SHA512.Create())
            {
                hashedPassword = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(password))).Replace("-", "");
            }
            if (hashedPassword == dt.Rows[0].Field<string>("PASSWORD"))
            {   //  Correct Password
                return userUID;
            }
            else
            {

                InvalidCredentials("Invalid Password.");
                return "";
            }
        }

        /// <summary>
        /// Tells the user their credentials are invalid
        /// </summary>
        /// <param name="message"></param>
        private void InvalidCredentials(string message)
        {
            Message_txt.Text = message;

            // Do some animation
        }

        private void New_User_btn_Click(object sender, RoutedEventArgs e)
        {
            Message_txt.Text = "";
            Debug.WriteLine("New User");
            frame.Navigate(new UserCreationPage(frame));
        }

        private void Exit_btn_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void username_txt_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
