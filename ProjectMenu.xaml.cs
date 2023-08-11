using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static ProjectSoft.DatabaseAccess;

namespace ProjectSoft
{
    /// <summary>
    /// Interaction logic for ProjectMenu.xaml
    /// </summary>
    public partial class ProjectMenu : Page
    {
        Frame frame;
        Project project;

        private ProjectTask selectedTask = null;

        ObservableCollection<Member> allMembers = new ObservableCollection<Member>();

        private bool visibleAccessEdit = false;

        public ProjectMenu(Frame frame, Project project)
        {
            this.frame = frame;
            this.project = project;

            project.LoadMembers();
            project.LoadTasks();

            InitializeComponent();

            if (project.GetUserAccess() >= Member.AccessType.Owner)
            {
                EditAccess_btn.Visibility = Visibility.Visible;
            }
            if(project.GetUserAccess() <= Member.AccessType.Viewer)
            {
                NewTask_btn.IsEnabled = false;
            }
            

            Task_ListView.ItemsSource = project.Tasks;
            MembersComboBox.ItemsSource = allMembers;
            SelectedMembersList.ItemTemplate = (DataTemplate)SelectedMembersList.FindResource("Viewer_style");
            SelectedMembersList.ItemsSource = project.Members;
        }

        private void LoadAllMembers()
        {
            allMembers.Clear();
            DataTable dt;
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = @"WITH temp AS
                                    (
	                                    SELECT [PROJECT_UID], [USER_UID] FROM [PROJECT_PERSONNEL] WHERE [PROJECT_UID] = @projectUid
                                    )

                                    SELECT users.[UID], users.[USERNAME], temp.[PROJECT_UID]
                                    FROM [USERS] users 
                                    LEFT JOIN temp ON users.[UID] = temp.[USER_UID] 
                                    WHERE temp.[PROJECT_UID] IS NULL";  


                cmd.Parameters.AddWithValue("@projectUid", project.Uid);

                dt = Select(cmd);
            }
            foreach (DataRow dr in dt.Rows)
            {
                allMembers.Add(new Member(dr.Field<string>("UID"), dr.Field<string>("USERNAME"), Member.AccessType.None, DateTime.MinValue));
            }
        }

        private void New_Task_btn_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Create new task");


            // Set up databindings for the popup

            selectedTask = new ProjectTask(project);
            //PopupDataGrid.DataContext = selectedTask;

            //TaskPopup.IsOpen = true;

            TaskWindow tw = new TaskWindow(project, selectedTask);
            tw.Show();

        }

        private void Task_Click(object sender, MouseButtonEventArgs e)
        {
            Debug.WriteLine("Edit Task");

            selectedTask = (ProjectTask)((Border)sender).DataContext;
            selectedTask.LoadMembers();
            //PopupDataGrid.DataContext = selectedTask;
            //TaskPopup.IsOpen = true; 
            TaskWindow tw = new TaskWindow(project, selectedTask);
            tw.Show();
        }



        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch(Project_TabControl.SelectedIndex)
            {
                case 0:
                    break;
                case 1:
                    if (e.AddedItems.Count > 0)
                    {
                        LoadAllMembers();   // Refresh all Members
                    }
                    break;
                case 2:
                    break;
            }
        }

        private void AddMember_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Adding Member to Project");
            string searchText = MembersComboBox.Text;
            Member selectedMember = allMembers.FirstOrDefault(member => member.Name == searchText);
            if (selectedMember != null)
            {
                project.Members.Add(selectedMember);
                allMembers.Remove(selectedMember);

                selectedMember.Access = Member.AccessType.Viewer;

                DataTable dt;
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.CommandText = @"SELECT [USER_UID] FROM [PROJECT_PERSONNEL] WHERE [PROJECT_UID] = @projectUid AND [USER_UID] = @userUid";


                    cmd.Parameters.AddWithValue("@projectUid", project.Uid);
                    cmd.Parameters.AddWithValue("@userUid", selectedMember.Uid);

                    dt = Select(cmd);
                }
                if(dt.Rows.Count == 0)
                {
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        cmd.CommandText = @"INSERT INTO [dbo].[PROJECT_PERSONNEL]
                                   ([PROJECT_UID]
                                   ,[USER_UID]
                                   ,[USER_ACCESS]
                                   ,[LAST_UPDATED])
                             VALUES
                                   (@projectUid
                                   ,@userUid
                                   ,@userAccess
                                   ,@lastUpdate)";

                        cmd.Parameters.AddWithValue("@projectUid", project.Uid);
                        cmd.Parameters.AddWithValue("@userUid", selectedMember.Uid);
                        cmd.Parameters.AddWithValue("@userAccess", selectedMember.Access.ToString());
                        cmd.Parameters.AddWithValue("@lastUpdate", DateTime.UtcNow);

                        Update(cmd);
                    }
                }
                else
                {
                    MessageBox.Show("An error occurred: The User: "+ selectedMember.Name + " is already added to this project" , "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void RemoveMember_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Remove Member from Project");
            Member member = (Member)((Button)sender).DataContext;

            MessageBoxResult result = MessageBox.Show("Are you sure you want to remove " + member.Name + " from the project?\n" +
                "This will remove them from all tasks as well.", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                //  Create command to remove the user's relation to the project
                SqlCommand cmdProject = new SqlCommand();
                cmdProject.CommandText = @"DELETE FROM [PROJECT_PERSONNEL] 
                                        WHERE [PROJECT_UID] = @projectUid AND [USER_UID] = @userUid";

                cmdProject.Parameters.AddWithValue("@projectUid", project.Uid);
                cmdProject.Parameters.AddWithValue("@userUid", member.Uid);

                //  Create the command to remove the user's relation to all tasks in this project
                SqlCommand cmdTasks = new SqlCommand();
                cmdTasks.CommandText = @"WITH temp AS
                                        (
	                                        SELECT * 
	                                        FROM [PROJECT_TASKS]
	                                        WHERE [PROJECT_UID] = @projectUid
                                        )

                                        DELETE FROM [TASK_PERSONNEL]
                                        FROM [TASK_PERSONNEL] as task_personnel
                                        LEFT JOIN temp ON temp.TASK_UID = task_personnel.TASK_UID
                                        WHERE task_personnel.USER_UID = @userUid";

                cmdTasks.Parameters.AddWithValue("@projectUid", project.Uid);
                cmdTasks.Parameters.AddWithValue("@userUid", member.Uid);

                try
                {
                    Transaction(cmdProject, cmdTasks);
                }
                catch (Exception ex)
                {
                    throw ex;
                }

                member.Access = Member.AccessType.None;
                project.Members.Remove(member);
                allMembers.Add(member);
            }
            else
            {
                return;
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (frame.CanGoBack)
                frame.NavigationService.GoBack();
            else
                App.Current.MainWindow.Close();
        }

        private void EditAccess_btn_Click(object sender, RoutedEventArgs e)
        {
            ToggleAccessEdit();
        }

        private void CancelChanges_btn_Click(object sender, RoutedEventArgs e)
        {
            //  Reset the temp variable if it was changed
            foreach(Member member in project.Members) 
            {
                member.tempAccess = member.Access;
            }
            ToggleAccessEdit();
        }

        private void ConfirmChanges_btn_Click(object sender, RoutedEventArgs e)
        {
            //  Submit access changes in member object
            foreach (Member member in project.Members)
            {
                if(member.Access != member.tempAccess)
                {
                    member.Access = member.tempAccess;
                    member.UpdateAccess(project.Uid);
                }
            }
            ToggleAccessEdit();
        }

        private void ToggleAccessEdit()
        {
            visibleAccessEdit = !visibleAccessEdit;
            SelectedMembersList.ItemTemplate = visibleAccessEdit ? (DataTemplate)SelectedMembersList.FindResource("AccessEdit_style") : (DataTemplate)SelectedMembersList.FindResource("Viewer_style");
            if (visibleAccessEdit)
            {
                EditAccess_btn.Visibility = Visibility.Hidden;
                ConfirmChanges_btn.Visibility = Visibility.Visible;
                CancelChanges_btn.Visibility = Visibility.Visible;
            }
            else
            {
                EditAccess_btn.Visibility = Visibility.Visible;
                ConfirmChanges_btn.Visibility = Visibility.Hidden;
                CancelChanges_btn.Visibility = Visibility.Hidden;
            }
        }


        //private void TaskPopupClose_Click(object sender, RoutedEventArgs e)
        //{
        //    Debug.WriteLine("Closing Task Popup");
        //    TaskPopup.IsOpen = false;
        //}

        //private void TaskPopupSubmit_Click(object sender, RoutedEventArgs e)
        //{
        //    Debug.WriteLine("Submitting Changes");

        //    selectedTask.Save();

        //    TaskPopup.IsOpen = false;

        //    // Refresh the Task_ListView to update the UI
        //    Task_ListView.Items.Refresh();
        //}
        //private void popup_AddMember_Click(object sender, RoutedEventArgs e)
        //{
        //    Debug.WriteLine("Adding Member to Task");
        //    string searchText = popup_MembersComboBox.Text;
        //    Member selectedMember = selectedTask.AvailableMembers.FirstOrDefault(member => member.Name == searchText);
        //    if (selectedMember != null)
        //    {
        //        selectedTask.Members.Add(selectedMember);
        //        selectedTask.AvailableMembers.Remove(selectedMember);

        //        DataTable dt;
        //        using (SqlCommand cmd = new SqlCommand())
        //        {
        //            cmd.CommandText = @"SELECT [USER_UID] FROM [TASK_PERSONNEL] WHERE [TASK_UID] = @taskUid AND [USER_UID] = @userUid";


        //            cmd.Parameters.AddWithValue("@taskUid", selectedTask.Uid);
        //            cmd.Parameters.AddWithValue("@userUid", selectedMember.Uid);

        //            dt = Select(cmd);
        //        }
        //        if (dt.Rows.Count == 0)
        //        {
        //            using (SqlCommand cmd = new SqlCommand())
        //            {
        //                cmd.CommandText = @"INSERT INTO [dbo].[TASK_PERSONNEL]
        //                           ([TASK_UID]
        //                           ,[USER_UID]
        //                           ,[LAST_UPDATED])
        //                     VALUES
        //                           (@taskUid
        //                           ,@userUid
        //                           ,@lastUpdate)";

        //                cmd.Parameters.AddWithValue("@taskUid", selectedTask.Uid);
        //                cmd.Parameters.AddWithValue("@userUid", selectedMember.Uid);
        //                cmd.Parameters.AddWithValue("@lastUpdate", DateTime.UtcNow);

        //                Update(cmd);
        //            }
        //        }
        //        else
        //        {
        //            MessageBox.Show("An error occurred: The User: " + selectedMember.Name + " is already added to this task", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //        }
        //    }
        //}

        //private void popup_RemoveMember_Click(object sender, RoutedEventArgs e)
        //{
        //    Debug.WriteLine("Remove Member from Task");
        //    Member member = (Member)((Button)sender).DataContext;

        //    MessageBoxResult result = MessageBox.Show("Are you sure you want to remove " + member.Name + " from the task?\n", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);

        //    if (result == MessageBoxResult.Yes)
        //    {
        //        //  Create command to remove the user's relation to the task
        //        SqlCommand cmdProject = new SqlCommand();
        //        cmdProject.CommandText = @"DELETE FROM [TASK_PERSONNEL] 
        //                                WHERE [TASK_UID] = @taskUid AND [USER_UID] = @userUid";

        //        cmdProject.Parameters.AddWithValue("@taskUid", selectedTask.Uid);
        //        cmdProject.Parameters.AddWithValue("@userUid", member.Uid);

        //        try
        //        {
        //            Transaction(cmdProject);
        //        }
        //        catch (Exception ex)
        //        {
        //            throw ex;
        //        }

        //        selectedTask.Members.Remove(member);
        //        selectedTask.AvailableMembers.Add(member);
        //    }
        //    else
        //    {
        //        return;
        //    }
        //}       
    }
}
