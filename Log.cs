using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ProjectSoft.DatabaseAccess;

namespace ProjectSoft
{
    public static class Log
    {
        public static void CreateEvent(EventType et, string? message, string? personUid, string? projectUid)
        {
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.CommandText = @"INSERT INTO [SYSTEM_LOGS] ([EVENT_TYPE]
                                   ,[TIME_OCCURRED]
                                   ,[MESSAGE]
                                   ,[USER_UID]
                                   ,[PROJECT_UID])
                             VALUES
                                   (@eventType
                                   ,@timeOccurred
                                   ,@message
                                   ,@userUid
                                   ,@projectUid)";

                cmd.Parameters.AddWithValue("@eventType", et.ToString());
                cmd.Parameters.AddWithValue("@timeOccurred", DateTime.UtcNow);
                cmd.Parameters.AddWithValue("@message", message);
                cmd.Parameters.AddWithValue("@userUid", personUid);
                cmd.Parameters.AddWithValue("@projectUid", projectUid);

                Update(cmd);
            }
        }


        public enum EventType
        {
            Login,
            AccessChanged,
            RecordChanged,
            QueryFail
        }
    }
}
