using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ProjectSoft.DatabaseAccess;

namespace ProjectSoft
{
    public class Member
    {
        public string Uid { get; }
        public string Name { get; set; }
        public AccessType Access { get; set; }
        public DateTime? LastUpdated { get; set; }

        public AccessType tempAccess { get; set; }

        public Member(string uid, string name, AccessType access, DateTime? lastUpdated)
        {
            this.Uid = uid;
            this.Name = name;
            this.Access = access;
            this.LastUpdated = lastUpdated;
            tempAccess = access;
        }

        public void UpdateAccess(string projectUid)
        {
            using(SqlCommand cmd = new SqlCommand())
            {
                cmd.CommandText = @"UPDATE [dbo].[PROJECT_PERSONNEL]
                                    SET [USER_ACCESS] = @userAccess
                                        ,[LAST_UPDATED] = @datetime
                                    WHERE [PROJECT_UID] = @projectUid 
                                    AND [USER_UID] = @userUid";

                cmd.Parameters.AddWithValue("@projectUid", projectUid);
                cmd.Parameters.AddWithValue("@userUid", Uid);
                cmd.Parameters.AddWithValue("@userAccess", Access.ToString());
                cmd.Parameters.AddWithValue("@datetime", DateTime.Now.ToString());

                Update(cmd);
            }
        }

        public static AccessType AccessParse(string? str)
        {
            switch (str)
            {
                case "None":
                case "NONE":
                    return AccessType.None;
                case "Viewer":
                case "VIEWER":
                    return AccessType.Viewer;
                case "Editor":
                case "EDITOR":
                    return AccessType.Editor;
                case "Owner":
                case "OWNER":
                    return AccessType.Owner;
                default:
                    return AccessType.None;
            }
        }
        public static IEnumerable<AccessType> AccessTypes
        {
            get
            {
                return Enum.GetValues(typeof(AccessType))
                           .Cast<AccessType>();
            }
        }
        public enum AccessType
        {
            None,
            Viewer,
            Editor,
            Owner
        }
    }
}
