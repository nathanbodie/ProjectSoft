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
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static ProjectSoft.DatabaseAccess;

namespace ProjectSoft
{
    /// <summary>
    /// Interaction logic for TaskWindow.xaml
    /// </summary>
    public partial class TaskWindow : Window
    {
        Project project;
        ProjectTask task;
        public TaskWindow(Project project, ProjectTask task)
        {
            this.project = project;
            this.task = task;
            task.LoadMembers();

            InitializeComponent();

            if(project.GetUserAccess() <= Member.AccessType.Viewer)
            {
                Submit_btn.IsEnabled = false;
            }

            PopupDataGrid.DataContext = task;
        }
        private void popup_AddMember_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Adding Member to Task");
            string searchText = popup_MembersComboBox.Text;
            Member selectedMember = task.AvailableMembers.FirstOrDefault(member => member.Name == searchText);
            if (selectedMember != null)
            {
                task.Members.Add(selectedMember);
                task.AvailableMembers.Remove(selectedMember);

                DataTable dt;
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.CommandText = @"SELECT [USER_UID] FROM [TASK_PERSONNEL] WHERE [TASK_UID] = @taskUid AND [USER_UID] = @userUid";


                    cmd.Parameters.AddWithValue("@taskUid", task.Uid);
                    cmd.Parameters.AddWithValue("@userUid", selectedMember.Uid);

                    dt = Select(cmd);
                }
                if (dt.Rows.Count == 0)
                {
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        cmd.CommandText = @"INSERT INTO [dbo].[TASK_PERSONNEL]
                                   ([TASK_UID]
                                   ,[USER_UID]
                                   ,[LAST_UPDATED])
                             VALUES
                                   (@taskUid
                                   ,@userUid
                                   ,@lastUpdate)";

                        cmd.Parameters.AddWithValue("@taskUid", task.Uid);
                        cmd.Parameters.AddWithValue("@userUid", selectedMember.Uid);
                        cmd.Parameters.AddWithValue("@lastUpdate", DateTime.UtcNow);

                        Update(cmd);
                    }
                }
                else
                {
                    MessageBox.Show("An error occurred: The User: " + selectedMember.Name + " is already added to this task", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void popup_RemoveMember_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Remove Member from Task");
            Member member = (Member)((Button)sender).DataContext;

            MessageBoxResult result = MessageBox.Show("Are you sure you want to remove " + member.Name + " from the task?\n", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                //  Create command to remove the user's relation to the task
                SqlCommand cmdProject = new SqlCommand();
                cmdProject.CommandText = @"DELETE FROM [TASK_PERSONNEL] 
                                        WHERE [TASK_UID] = @taskUid AND [USER_UID] = @userUid";

                cmdProject.Parameters.AddWithValue("@taskUid", task.Uid);
                cmdProject.Parameters.AddWithValue("@userUid", member.Uid);

                try
                {
                    Transaction(cmdProject);
                }
                catch (Exception ex)
                {
                    throw ex;
                }

                task.Members.Remove(member);
                task.AvailableMembers.Add(member);
            }
            else
            {
                return;
            }
        }

        private void TaskPopupSubmit_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Submitting Changes");

            task.Save();

            this.Close();

            // Refresh the Task_ListView to update the UI
            //Task_ListView.Items.Refresh();
        }
        private void TaskPopupClose_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Closing Task Popup");
            this.Close();
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
