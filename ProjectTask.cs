using Microsoft.Data.SqlClient;
using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Security.RightsManagement;
using System.Text;
using System.Threading.Tasks;
using static ProjectSoft.DatabaseAccess;

namespace ProjectSoft
{
    public class ProjectTask 
    {
        //  Database Fields
        public string Uid { get; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        //public string Type { get; set; }
        public TaskStatus Status { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? CompletedDate { get; set; }

        public ObservableCollection<Member> Members { get; set; }
        public ObservableCollection<Member>? AvailableMembers { get; set; }

        // Other variables
        public Project project { get; set; }

        public List<string> membersUids = new List<string>();

        private string? searchText;
        public string? SearchText
        {
            get { return searchText; }
            set
            {
                searchText = value;
                //OnPropertyChanged(nameof(SearchText));
            }
        }

        public ProjectTask(string uid, string name, string description, TaskStatus status, DateTime dueDate, Project project)
        {
            Uid = uid;
            Name = name;
            Description = description;
            Status = status;
            DueDate = dueDate;

            this.project = project;
        }

        public ProjectTask(Project project) : this(Guid.NewGuid().ToString(), "New Task", string.Empty, TaskStatus.NotStarted, DateTime.Today.AddDays(7), project)
        { }

        

        public ProjectTask(string uid, Project project)
        {
            if (uid == null)
                throw new Exception("Task UID cannot be NULL");
            DataTable dt;
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "SELECT * From [dbo].[TASKS] WHERE [UID] = @uid";

                cmd.Parameters.AddWithValue("@uid", uid);
                dt = Select(cmd);
            }
            if (dt.Rows.Count == 0)
            {
                throw new Exception("Task with UID '" + uid + "' not found.");
            }
            this.Uid = uid;
            this.Name = dt.Rows[0].Field<string>("NAME");
            this.Description = dt.Rows[0].Field<string>("DESCRIPTION");
            this.Status = StatusParse(dt.Rows[0].Field<string?>("STATUS"));
            this.DueDate = dt.Rows[0].Field<DateTime?>("DUE_DATE");
            this.CompletedDate = dt.Rows[0].Field<DateTime?>("COMPLETED_DATE");

            this.project = project;
        }

        private void AddProjectMember(object obj)
        {
            // Add member to selected members collection
            var selectedMember = AvailableMembers.FirstOrDefault(m => m.Name.Contains(SearchText));
            if (selectedMember != null)
            {
                Members.Add(selectedMember);
                SearchText = string.Empty; // Clear search text
            }
        }


        public void LoadMembers()
        {
            Members = new ObservableCollection<Member>();
            AvailableMembers = new ObservableCollection<Member>(project.Members);

            DataTable dt;
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = @"SELECT [USER_UID]
                                FROM [TASK_PERSONNEL]
                                WHERE [TASK_UID] = @taskUid";

                cmd.Parameters.AddWithValue("@taskUid", this.Uid);

                dt = Select(cmd);
            }
            foreach(DataRow dr in dt.Rows)
            {
                Member m = project.Members.FirstOrDefault(m => m.Uid == dr.Field<string>("USER_UID"));

                Members.Add(m);
                AvailableMembers.Remove(m);
                //membersUids.Add(dr.Field<string>("USER_UID"));
            }
            //using (SqlCommand cmd = new SqlCommand())
            //{
            //    cmd.CommandType = CommandType.Text;
            //    cmd.CommandText = @"SELECT personnel.user_uid as USER_UID, users.USERNAME as NAME, personnel.USER_ACCESS as ACCESS
            //            FROM [TASK_PERSONNEL] personnel
            //            JOIN [USERS] users ON personnel.user_uid = users.UID
            //            WHERE personnel.project_uid = @taskUid";

            //    cmd.Parameters.AddWithValue("@taskUid", this.uid);
            //    dt = Select(cmd);
            //}
            //foreach (DataRow dr in dt.Rows)
            //{
            //    Member member = new Member(dr.Field<string>("USER_UID"), dr.Field<string>("NAME"), Member.AccessParse(dr.Field<string?>("ACCESS")));
            //    Members.Add(member);
            //}            
        }

        /// <summary>
        /// Handles updating the record in the database.
        /// </summary>
        public void Save()
        {
            if (Uid == null)
                throw new Exception("Task UID cannot be NULL");

            //  Create queries for handling Members
            List<SqlCommand> commands = new List<SqlCommand>();

            DataTable dt;
            //  Check if record for this task already exists.
            using (SqlCommand query = new SqlCommand())
            {
                query.CommandType = CommandType.Text;
                query.CommandText = "SELECT [UID] From [dbo].[TASKS] WHERE [UID] = @uid";

                query.Parameters.AddWithValue("@uid", this.Uid);
                dt = Select(query);
            }
            SqlCommand cmd = new SqlCommand();            
            cmd.CommandType = CommandType.Text;

            //  If this record already exists, update the currect record otherwise create a new one.
            if (dt.Rows.Count == 0)
            {
                //  Create a new entry in the database.
                Debug.WriteLine("Creating new Task Record");

                cmd.CommandText = @" INSERT INTO [dbo].[TASKS] (UID, NAME, DESCRIPTION, STATUS, DUE_DATE, COMPLETED_DATE) 
                            VALUES (@uid, @Name, @Description, @Status, @DueDate, @DateCompleted);";
            }

            else
            {
                // Update the current record.
                cmd.CommandText = @"UPDATE [dbo].[TASKS] SET 
                NAME = @Name,
                DESCRIPTION = @Description,
                STATUS = @Status,
                DUE_DATE = @DueDate,
                COMPLETED_DATE = @DateCompleted
                WHERE [UID] = @uid";
            }
            cmd.Parameters.AddWithValue("@Name", this.Name);
            cmd.Parameters.AddWithValue("@Description", this.Description);
            cmd.Parameters.AddWithValue("@Status", this.Status.ToString());
            cmd.Parameters.AddWithValue("@DueDate", this.DueDate);
            cmd.Parameters.AddWithValue("@DateCompleted", CompletedDate == null ? DBNull.Value : CompletedDate);
            cmd.Parameters.AddWithValue("@uid", this.Uid);

            commands.Add(cmd);

            //  If this is a new record, add a new relation to the project in the database. 
            if(dt.Rows.Count == 0)
            {
                //  Create relation to project
                SqlCommand relCmd = new SqlCommand();

                relCmd.CommandType = CommandType.Text;
                relCmd.CommandText = @"INSERT INTO [dbo].[PROJECT_TASKS] (PROJECT_UID, TASK_UID)
                    VALUES (@projectUid, @taskUid);";

                relCmd.Parameters.AddWithValue("@projectUid", this.project.Uid);
                relCmd.Parameters.AddWithValue("@taskUid", this.Uid);
                commands.Add(relCmd);
                
                                   
            }
            //  Save Members relations
            SqlCommand memCmd = new SqlCommand();
            memCmd.CommandType = CommandType.Text;

            try
            {
                Transaction(commands.ToArray());

                // Add the task to the current project, so it shows it shows up without having to reload the data.
                if (project != null)
                    project.Tasks.Add(this);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            
        }

        public List<SqlCommand> SaveMembers()
        {
            List<SqlCommand> commands = new List<SqlCommand>();
            DataTable dt = new DataTable();
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = @"SELECT * FROM [TASK_PERSONNEL] WHERE [TASK_UID] = @taskUid";

                cmd.Parameters.AddWithValue("@taskUid", this.Uid);

                dt = Select(cmd);
            }
            //  In this section, we need to add newly added members to the relation table, remove members who are newly removed,
            //  and update the access of members whos access has changed.
            foreach(string member in membersUids) 
            {
                bool exists = false;
                // Check if the member is in database
                foreach (DataRow dr in dt.Rows)
                {
                    if(dr.Field<string>("USER_UID") == member)
                    {   //  This record exists already. We can check to see if the record needs updating
                        //if(dr.Field<string>("ACCESS") != member.access.ToString())
                        //{   // If the access is changed, we create a query to update the record.
                        //    SqlCommand cmd = new SqlCommand();
                        //    cmd.CommandType = CommandType.Text;
                        //    cmd.CommandText = @"UPDATE [dbo].[TASK_PERSONNEL] SET
                        //                        [ACCESS] = @access,
                        //                        [LAST_UPDATED] = @updateTime,
                        //                        WHERE [USER_UID] = @userUid AND [TASK_UID] = @taskUid";

                        //    cmd.Parameters.AddWithValue("@access", member.access.ToString());
                        //    cmd.Parameters.AddWithValue("@updateTime", DateTime.UtcNow.ToString());
                        //    cmd.Parameters.AddWithValue("@userUid", member.uid);
                        //    cmd.Parameters.AddWithValue("@taskUid", this.uid);

                        //    commands.Add(cmd);
                        //}

                        //  Since we have matched the record in the database to, we can remove it from the datatable
                        //  To speed up further searches and break to go to the next member in the ProjectTask.
                        dt.Rows.Remove(dr);
                        exists = true;
                        break;
                    }
                }
                if(!exists)
                {   //  If the record doesn't exist in the DB currently, then create a new record.
                    SqlCommand cmd = new SqlCommand();
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = @"INSERT INTO [dbo].[TASK_PERSONNEL] (TASK_UID, USER_UID, LAST_UPDATED)
                            VALUES (@taskUid, @userUid, @updateTime);";

                    cmd.Parameters.AddWithValue("@taskUid", this.Uid);
                    cmd.Parameters.AddWithValue("@userUid", member);
                    cmd.Parameters.AddWithValue("@updateTime", DateTime.UtcNow.ToString());
                    commands.Add(cmd);
                }
            }      
            foreach (DataRow dr in dt.Rows )
            {   //  These are all the rows that are in the database, but are no longer
                //  attactched to the task. They are now removed from the database
                SqlCommand deleteCmd = new SqlCommand();
                deleteCmd.CommandType = CommandType.Text;
                deleteCmd.CommandText = @"DELETE FROM [TASK_PERSONNEL] WHERE [TASK_UID] = @taskUid AND [USER_UID] = @userUid";

                deleteCmd.Parameters.AddWithValue("@taskUid", this.Uid);
                deleteCmd.Parameters.AddWithValue("@userUid", dr.Field<string>("USER_UID"));

                commands.Add(deleteCmd);
            }

            return commands;
        }

        public enum TaskStatus
        {
            [Description ("Unknown")]
            Unknown,
            [Description("Not Started")]
            NotStarted,
            [Description("Active")]
            Active,
            [Description("Complete")]
            Completed,
            [Description("On Hold")]
            OnHold,         
        }
        public static IEnumerable<TaskStatus> TaskStatuses
        {
            get
            {
                return Enum.GetValues(typeof(TaskStatus))
                           .Cast<TaskStatus>();
            }
        }

        public static TaskStatus StatusParse(string? str)
        {
            switch (str)
            {
                case "Not Started":
                case "NotStarted":
                    return TaskStatus.NotStarted;
                case "Active":
                    return TaskStatus.Active;
                case "Completed":
                    return TaskStatus.Completed;
                case "On Hold":
                case "OnHold":
                    return TaskStatus.OnHold;
                default:
                    return 0;
            }
        }
    }
}
