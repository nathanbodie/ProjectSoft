using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static ProjectSoft.DatabaseAccess;

namespace ProjectSoft
{
    public class Project
    {
        public string Uid { get; set; }
        public string? Name { get; set; }
        public string? Org_UID { get; set; }
        public DateTime? Started { get; set; }
        public DateTime? Completed { get; set; }
        public List<ProjectTask> Tasks = new List<ProjectTask>();

        public ObservableCollection<Member> Members = new ObservableCollection<Member>();

        private Member.AccessType activeUserAccess = Member.AccessType.None;

        //public DateTime? LastUpdated { get; set;}

        /// <summary>
        /// Default contructor. Creates empty Project
        /// </summary>
        public Project() {
            this.Uid = "";
            this.Name = "";
            this.Org_UID = "";
            this.Started = DateTime.Now;
            this.Completed = null;
        }

        public Project(string UID, string name, string org_UID, DateTime started, DateTime? completed) 
        { 
            this.Uid = UID;
            this.Name = name;
            this.Org_UID = org_UID;
            this.Started = started;
            this.Completed = completed;
        }
        /// <summary>
        /// Load existing Project from database
        /// </summary>
        /// <param name="UID"></param>
        /// <exception cref="Exception"></exception>
        public Project(string? uid)
        {
            if (uid == null)
                throw new Exception("Project UID cannot be NULL");

            // Load project fields
            DataTable dt;
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "SELECT * FROM [dbo].[PROJECTS] WHERE [UID] = @uid";

                cmd.Parameters.AddWithValue("@uid", uid);
                dt = Select(cmd);
            }
            if (dt.Rows.Count == 0)
            {
                throw new Exception("Project with UID '" + uid + "' was not found.");
            }
            this.Uid = uid;
            this.Name = dt.Rows[0].Field<string>("NAME");
            this.Org_UID = dt.Rows[0].Field<string>("ORGANIZATION_UID");
            this.Started = dt.Rows[0].Field<DateTime?>("DATE_STARTED");
            Completed = null;
            this.Completed = dt.Rows[0].Field<DateTime?>("DATE_COMPLETED");            
        }
        /// <summary>
        /// Loads asssociated tasks from the database
        /// </summary>
        public void LoadTasks()
        {
            DataTable dt;
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "SELECT [TASK_UID] FROM [dbo].[PROJECT_TASKS] WHERE [PROJECT_UID] = @projectUid";

                cmd.Parameters.AddWithValue("@projectUid", this.Uid);
                dt = Select(cmd);
            }
            foreach (DataRow dr in dt.Rows)
            {
                ProjectTask pt = new ProjectTask(dr.Field<string>("TASK_UID"), this);
                Tasks.Add(pt);
            }
        }

        // Loads members of a project
        public void LoadMembers()
        {
            Members.Clear();
            DataTable dt;
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = @"SELECT personnel.user_uid as USER_UID, users.USERNAME as NAME, personnel.USER_ACCESS as ACCESS, personnel.LAST_UPDATED
                    FROM [PROJECT_PERSONNEL] personnel
                    JOIN [USERS] users ON personnel.user_uid = users.UID
                    WHERE personnel.project_uid = @projectUid";

                cmd.Parameters.AddWithValue("@projectUid", this.Uid);

                dt = Select(cmd);
            }
            foreach (DataRow dr in dt.Rows)
            {
                Member member = new Member(dr.Field<string>("USER_UID"), dr.Field<string>("NAME"), Member.AccessParse(dr.Field<string>("ACCESS")), dr.Field<DateTime?>("LAST_UPDATED"));
                Members.Add(member);
            }
        }

        public void Save()
        {
            throw new NotImplementedException();
        }

        public List<SqlCommand> SaveMembers()
        {
            List<SqlCommand> commands = new List<SqlCommand>(); //  Creates a list of SQL commands, so they can be transacted together.
            DataTable dt = new DataTable();
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = @"SELECT * FROM [PROJECT_PERSONNEL] WHERE [PROJECT_UID] = @projectUid";

                cmd.Parameters.AddWithValue("@projectUid", this.Uid);

                dt = Select(cmd);
            }
            //  In this section, we need to add newly added members to the relation table, remove members who are newly removed,
            //  and update the access of members whos access has changed.
            foreach (Member member in Members)
            {
                bool exists = false;
                // Check if the member is in database
                foreach (DataRow dr in dt.Rows)
                {
                    if (dr.Field<string>("USER_UID") == member.Uid)
                    {   //  This record exists already. We can check to see if the record needs updating
                        if (dr.Field<string>("ACCESS") != member.Access.ToString())
                        {   // If the access is changed, we create a query to update the record.
                            SqlCommand cmd = new SqlCommand();
                            cmd.CommandType = CommandType.Text;
                            cmd.CommandText = @"UPDATE [dbo].[PROJECT_PERSONNEL] SET
                                                [ACCESS] = @access,
                                                [LAST_UPDATED] = @updateTime,
                                                WHERE [USER_UID] = @userUid AND [TASK_UID] = @projectUid";

                            cmd.Parameters.AddWithValue("@access", member.Access.ToString());
                            cmd.Parameters.AddWithValue("@updateTime", DateTime.UtcNow.ToString());
                            cmd.Parameters.AddWithValue("@userUid", member.Uid);
                            cmd.Parameters.AddWithValue("@projectUid", this.Uid);

                            commands.Add(cmd);
                        }

                        //  Since we have matched the record in the database to, we can remove it from the datatable
                        //  To speed up further searches and break to go to the next member in the Project.
                        dt.Rows.Remove(dr);
                        exists = true;
                        break;
                    }
                }
                if (!exists)
                {   //  If the record doesn't exist in the DB currently, then create a new record.
                    SqlCommand cmd = new SqlCommand();
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = @"INSERT INTO [dbo].[PROJECT_PERSONNEL] (PROJECT_UID, USER_UID, LAST_UPDATED)
                            VALUES (@projectUid, @userUid, @updateTime);";

                    cmd.Parameters.AddWithValue("@projectUid", this.Uid);
                    cmd.Parameters.AddWithValue("@userUid", member.Uid);
                    cmd.Parameters.AddWithValue("@updateTime", DateTime.UtcNow.ToString());
                    commands.Add(cmd);
                }
            }
            foreach (DataRow dr in dt.Rows)
            {   //  These are all the rows that are in the database, but are no longer
                //  attactched to the task. They are now removed from the database
                SqlCommand deleteCmd = new SqlCommand();
                deleteCmd.CommandType = CommandType.Text;
                deleteCmd.CommandText = @"DELETE FROM [PROJECT_PERSONNEL] WHERE [PROJECT_UID] = @projectUid AND [USER_UID] = @userUid";

                deleteCmd.Parameters.AddWithValue("@projectUid", this.Uid);
                deleteCmd.Parameters.AddWithValue("@userUid", dr.Field<string>("USER_UID"));

                commands.Add(deleteCmd);
            }

            return commands;
        }

        public void SetUserAccess(string userUid)
        {
            DataTable dt = new DataTable();
            using(SqlCommand cmd = new SqlCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = @"SELECT [USER_ACCESS] 
                                    FROM [PROJECT_PERSONNEL] 
                                    WHERE [PROJECT_UID] = @projectUid
                                    AND [USER_UID] = @userUid";

                cmd.Parameters.AddWithValue("@projectUid", this.Uid);
                cmd.Parameters.AddWithValue("@userUid", userUid);

                dt = Select(cmd);
            }

            if (dt.Rows.Count > 0)
            {
                activeUserAccess = Member.AccessParse(dt.Rows[0].Field<string?>("USER_ACCESS"));
            }
        }
        public Member.AccessType GetUserAccess()
        {
            return activeUserAccess;
        }
    }

    public class EmptyProject : Project
    {
        public EmptyProject() : base("", "", "", DateTime.Now, DateTime.Now) { }
    }
}
