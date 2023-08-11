using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Resources;
using System.Reflection;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Configuration;

namespace ProjectSoft
{
    static class DatabaseAccess
    {

        static string connectionString = ConfigurationManager.ConnectionStrings["ProjectSoft.Properties.Settings.dbconnection"].ConnectionString;

        /// <summary>
        /// Executes Select queries
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static DataTable Select(string query)
        {
            DataTable dataTable = new DataTable();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(query, connection);
                //command.Parameters.AddWithValue("@paramName", "Parm-Value");
                SqlDataAdapter adapter = new SqlDataAdapter(command);
                try
                {
                    connection.Open();

                    adapter.Fill(dataTable);
                }
                catch(Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }
                finally
                {
                    connection.Close();
                    adapter.Dispose();
                }               
            }
            return dataTable;
        }

        public static DataTable Select(SqlCommand command)
        {
            DataTable dataTable = new DataTable();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                command.Connection = connection;
                SqlDataAdapter adapter = new SqlDataAdapter(command);
                try
                {
                    connection.Open();

                    adapter.Fill(dataTable);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                    throw ex;
                }
                finally
                {
                    connection.Close();
                    adapter.Dispose();
                }
            }
            return dataTable;
        }

        /// <summary>
        /// Executes Insert, Edit, or delete queries
        /// </summary>
        /// <param name="query"></param>
        public static void Update(string query)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(query, connection);
                //command.Parameters.AddWithValue("@paramName", "Parm-Value");
                try
                {
                    connection.Open();
                    command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                    throw ex;
                }
                finally
                {
                    connection.Close();
                }
            }
        }

        /// <summary>
        /// Executes Insert, Edit, or delete queries using a preassembed SqlCommand Object
        /// </summary>
        /// <param name="command">SqlCommand to execute</param>
        public static void Update(SqlCommand command)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                command.Connection = connection;
                try
                {
                    connection.Open();
                    command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                    throw ex;
                }
                finally
                {
                    connection.Close();
                }
            }
        }

        public static void Transaction(params SqlCommand[] cmds)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    using (SqlTransaction transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            foreach (SqlCommand cmd in cmds)
                            {
                                cmd.Connection = connection;
                                cmd.Transaction = transaction;
                                cmd.ExecuteNonQuery();
                            }

                            transaction.Commit();
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("Transaction Commit Failed: " + ex.Message);
                            transaction.Rollback();
                            throw ex;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Cannot connect to Database: " + ex.Message);
                    throw ex;
                }
                finally
                {
                    connection.Close();
                    foreach(SqlCommand cmd in cmds)
                    {
                        if(cmd != null)
                            cmd.Dispose();
                    }
                }
            }
        }

        /// <summary>
        /// Sanitizes a given string
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string? Sanitize(string? str)
        {
            //// Remove non-alpanumeric characters
            //string sanitized = Regex.Replace(str, @"[^a-zA-Z0-9]", "");
            //return sanitized;

            return str;
        }
    }
}
