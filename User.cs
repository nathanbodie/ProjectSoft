using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using static ProjectSoft.DatabaseAccess;
using Microsoft.Data.SqlClient;

namespace ProjectSoft
{
    public class User
    {
        public string Uid { get; set; }
        public string Username { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        public List<Project> Projects { get; }

        //  Basic Contructor
        public User(string username, string firstName, string lastName)
        {
            this.Uid = Guid.NewGuid().ToString();
            this.Username = username;
            this.FirstName = firstName;
            this.LastName = lastName;
        }

        //  Get user from Database using UID
        public User(string UID)
        {
            if (UID == null)
                throw new Exception("User UID cannot be NULL");
            DataTable dt = Select("SELECT * From [dbo].[USERS] WHERE [UID] = '" + UID + "'");
            if (dt.Rows.Count == 0)
            {
                throw new Exception("User with UID '" + UID + "' not found.");
            }
            this.Uid = UID;
            this.Username = dt.Rows[0].Field<string>("USERNAME");
            this.FirstName = dt.Rows[0].Field<string>("NAME_FIRST");
            this.LastName = dt.Rows[0].Field<string>("NAME_LAST");
        }

        public void LoadProjects()
        {
            DataTable dt;
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "SELECT [PROJECT_UID] FROM [dbo].[PROJECT_PERSONNEL] WHERE [USER_UID] = @userUid";

                cmd.Parameters.AddWithValue("@userUid", this.Uid);
                dt = Select(cmd);
            }
            foreach (DataRow dr in dt.Rows)
            {
                Project proj = new Project(dr.Field<string>("PROJECT_UID"));
                Projects.Add(proj);
            }
        }

        /// <summary>
        /// Updates the User's data in the database with the data in this object
        /// </summary>
        /// <returns>Returns True if the update was successful</returns>
        public bool Update()
        {
            throw new NotImplementedException();
            return false;
        }
    }
}
