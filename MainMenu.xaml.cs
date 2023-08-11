using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static ProjectSoft.DatabaseAccess;

namespace ProjectSoft
{
    /// <summary>
    /// Interaction logic for MainMenu.xaml
    /// </summary>
    public partial class MainMenu : Page
    {
        private Frame frame;
        User activeUser;
        List<Project> userProjects = new List<Project>();
        public MainMenu(Frame frame, string userUID)
        {
            this.frame = frame;
            InitializeComponent();
            activeUser = new User(userUID);
            LoadUserProjects();
            projectList.ItemsSource = userProjects;
        }

        // Loads user projects
        private void LoadUserProjects()
        {
            DataTable dt;
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "SELECT [PROJECT_UID] FROM [dbo].[PROJECT_PERSONNEL] WHERE [USER_UID] = @userUid";

                cmd.Parameters.AddWithValue("@userUid", activeUser.Uid);
                dt = Select(cmd);
            }
            foreach (DataRow dr in dt.Rows)
            {
                Project proj = new Project(dr.Field<string>("PROJECT_UID"));
                userProjects.Add(proj);
            }

            // New button
            userProjects.Add(new EmptyProject());
        }

        // Activates navigation on stack panel click
        private void StackPanel_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            object dataItem = ((FrameworkElement)sender).Tag;
            if(dataItem == null) 
            {
                return;
            }

            if(dataItem is Project)
            {
                // Navigate to Project view
                Debug.WriteLine("Navigating to Project: " +  ((Project)dataItem).Name);
                ((Project)dataItem).SetUserAccess(activeUser.Uid);
                frame.Navigate(new ProjectMenu(frame, (Project)dataItem));
            }
        }

        // Brings up new project creation popup
        private void New_Project_btn(object sender, MouseButtonEventArgs e)
        {
            Debug.WriteLine("Create new project");

            New_Project_Popup.IsOpen = true;
        }

        // Closese project creation popup
        private void Cancel_btn_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Close popup window");
            New_Project_Popup.IsOpen = false;
        }

        // Creates a project
        private void Create_btn_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Create New Project");
            
            //  Set up new project
            Project project = new Project(Guid.NewGuid().ToString(), ProjectName_txtbox.Text, null, DateTime.UtcNow, null);

            SqlCommand cmdproject = new SqlCommand();
            cmdproject.CommandText = @"INSERT INTO [PROJECTS] ([UID]
                                   ,[NAME]
                                   ,[DATE_STARTED])
                             VALUES
                                   (@projectUid
                                   ,@name
                                   ,@date)";

            cmdproject.Parameters.AddWithValue("@projectUid", project.Uid);
            cmdproject.Parameters.AddWithValue("@name", project.Name);
            cmdproject.Parameters.AddWithValue("@date", project.Started);

            SqlCommand cmdPersonnel = new SqlCommand();

            cmdPersonnel.CommandText = @"INSERT INTO [dbo].[PROJECT_PERSONNEL]
                                       ([PROJECT_UID]
                                       ,[USER_UID]
                                       ,[USER_ACCESS]
                                       ,[LAST_UPDATED])
                                 VALUES
                                       (@projectUid
                                       ,@userUid
                                       ,@access
                                       ,@updated)";

            cmdPersonnel.Parameters.AddWithValue("@projectUid", project.Uid);
            cmdPersonnel.Parameters.AddWithValue("@userUid", activeUser.Uid);
            cmdPersonnel.Parameters.AddWithValue("@access", Member.AccessType.Owner);
            cmdPersonnel.Parameters.AddWithValue("@updated", DateTime.UtcNow);

            Transaction(cmdproject, cmdPersonnel);

            userProjects.Add(project); 
            projectList.Items.Refresh();

            New_Project_Popup.IsOpen = false;
        }

        private void orgList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            activeUser = null;
            if(frame.CanGoBack)
                frame.NavigationService.GoBack();
            else
                App.Current.MainWindow.Close();
        }

        //private void Information_btn_Click(object sender, RoutedEventArgs e)
        //{
        //    Debug.WriteLine("Open information popup");
        //    Information_Popup.IsOpen = true;
        //}

        //private void Cancel_Information_btn_Click(object sender, RoutedEventArgs e)
        //{
        //    Debug.WriteLine("Close information popup");
        //    Information_Popup.IsOpen = false;
        //}
    }
}
