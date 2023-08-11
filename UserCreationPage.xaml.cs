using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
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
using static ProjectSoft.DatabaseAccess;


namespace ProjectSoft
{
    /// <summary>
    /// Interaction logic for UserCreationPage.xaml
    /// </summary>
    public partial class UserCreationPage : Page
    {
        Frame frame;
        public UserCreationPage(Frame frame)
        {
            this.frame = frame;
            InitializeComponent();
        }

        private void Create_User_btn_Click(object sender, RoutedEventArgs e)
        {
            //  <TODO> Add name fields to user profile and creation page

            PrintError("");
            //   Check if username is unique
            if(username_txt.Text == "")
            {
                PrintError("Username cannot be null.");
                return;
            }
            if (Select("SELECT [USERNAME] FROM [USERS] WHERE [USERNAME] = '" + username_txt.Text + "'").Rows.Count != 0)
            {
                PrintError("Username is already in use.");
                return;
            }

            //  Ensure the password is not null
            if (user_password_txt.Password == "")
            {
                PrintError("Password cannot be null.");
                return;
            }

            //  Ensure the password meets length requirements
            if (user_password_txt.Password.Length < 8)
            {
                PrintError("Password must be a minimum of 8 characters.");
                return;
            }

            //  Verify that the passwords are identical
            if (user_password_txt.Password != user_password_repeat_txt.Password)
            {
                PrintError("Passwords must match.");
                return;
            }

            //  Create UID
            string userUID = Guid.NewGuid().ToString();

            try
            {
                //  Insert new user
                string insertStatement = "INSERT INTO [USERS] (UID, USERNAME, NAME_FIRST, NAME_LAST, LAST_ACTIVE) VALUES (@Uid, @Username, @FirstName, @LastName, @LastActive)";

                // create a new SqlCommand object with the insert statement
                SqlCommand command = new SqlCommand(insertStatement);

                // add parameter values to the SqlCommand object
                command.Parameters.AddWithValue("@Uid", userUID);
                command.Parameters.AddWithValue("@Username", username_txt.Text);
                command.Parameters.AddWithValue("@FirstName", "");
                command.Parameters.AddWithValue("@LastName", "");
                command.Parameters.AddWithValue("@LastActive", DateTime.Now);

                Update(command);

                //  Insert password
                string hashedPassword;
                using (SHA512 sha = SHA512.Create())
                {
                    hashedPassword = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(user_password_repeat_txt.Password))).Replace("-", "");
                }

                insertStatement = "INSERT INTO [CREDENTIALS] (USER_UID, PASSWORD) VALUES (@Uid, @Password)";

                // create a new SqlCommand object with the insert statement
                command = new SqlCommand(insertStatement);

                // add parameter values to the SqlCommand object
                command.Parameters.AddWithValue("@Uid", userUID);
                command.Parameters.AddWithValue("@Password", hashedPassword);

                Update(command);

                frame.NavigationService.GoBack();

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                PrintError("User Creation Failed...");
            }


        }

        private void PrintError(string message)
        {
            Message_txt.Text = message;

            // Do some animation
        }

        private void Back_btn_Click(object sender, RoutedEventArgs e)
        {
            frame.NavigationService.GoBack();
        }
    }
}
