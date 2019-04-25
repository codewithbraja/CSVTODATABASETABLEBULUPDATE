using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectCSVTODB
{
    class Program
    {
        #region Properties
        public static string mHROSpath = string.Empty;
        public static string mInputFilepath = string.Empty;
        public static string mArchivePath = string.Empty;
        public static string mErrorPath = string.Empty;
        public static string mLogpath = string.Empty;
        public static string mWatchMask = string.Empty;
        public static string[] DefaultFolderStruct = { };
        public static string mDBConnectionString = string.Empty;

        public static string[] TableNames = { "tblinv", "STORE", "tblPO" };

        #endregion
        static void Main(string[] args)
        {
            process();
        }
        private static void process()
        {
            readAppSettings();
            try
            {
                Common.LogToFile(mLogpath, String.Format("Looking for CSV Data ..."), 1);

                #region Check path or Create path
                CheckPathDetails(DefaultFolderStruct);
                #endregion

                #region Get Files

                DirectoryInfo dirInfo = new DirectoryInfo(mInputFilepath);

                FileInfo[] DCWReports;
                DCWReports = dirInfo.GetFiles(mWatchMask);

                Common.LogToFile(mLogpath, String.Format("Number of CSV File found: {0}", DCWReports.Length.ToString()), 1);

                #endregion

                #region Process Script Reports

                foreach (FileInfo report in DCWReports)
                {

                    try
                    {
                        Common.LogToFile(mLogpath, String.Format("Starts Processing file: {0}\n", report.Name), 1);
                        SaveFileToDB(report.FullName);
                        Common.LogToFile(mLogpath, String.Format("Ends Processing file: {0}\n", report.Name), 1);
                        File.Move(report.FullName, Path.Combine(mArchivePath, Path.GetFileName(report.FullName)));

                    }
                    catch (Exception ex)
                    {
                        #region Move to error folder
                        Common.LogToFile(mLogpath, String.Format("Error while processing file: {0}. error - {1}", report.FullName, ex.Message), 4);
                        File.Move(report.FullName, Path.Combine(mErrorPath, Path.GetFileName(report.FullName)));
                        Environment.Exit(0);
                        #endregion
                    }
                }
                #endregion

            }
            catch (Exception ex)
            {
                Common.LogToFile(mLogpath, String.Format("Error: {0}", ex.Message), 4);
            }
        }


        private static void readAppSettings()
        {

            try
            {
                mHROSpath = ConfigurationManager.AppSettings["HROSPath"];
                mInputFilepath = ConfigurationManager.AppSettings["InputCSVPath"];
                mArchivePath = ConfigurationManager.AppSettings["ArchivePath"];
                mLogpath = ConfigurationManager.AppSettings["LogPath"];
                mErrorPath = ConfigurationManager.AppSettings["ErrorPath"];
                mWatchMask = ConfigurationManager.AppSettings["WatchMask"];
                mDBConnectionString = ConfigurationManager.AppSettings["DBConnectionString"];
                DefaultFolderStruct = new string[] { mHROSpath, mInputFilepath, mArchivePath, mErrorPath, mLogpath };
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("Error while reading Application settings. Error: {0}", ex.Message));
            }

        }
        private static void CheckPathDetails(string[] DefFoldStruct)
        {
            try
            {
                for (int i = 0; i < DefFoldStruct.Count(); i++)
                {
                    string path = DefFoldStruct[i];
                    if (!string.IsNullOrEmpty(path))
                    {
                        if (!Directory.Exists(path))
                            Directory.CreateDirectory(path);
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogToFile(mLogpath, String.Format("Error while creating relevant folders for Load CSV Application.. - {0}", ex.Message), 4);
            }
        }
        private static void SaveFileToDB(string CSVPath)
        {
            if (Path.GetFileName(CSVPath.ToUpper()).StartsWith("INVSUM"))
            {
                isActiveSetToZero("INVSUMtable");
                INVSUMToDatabase(CSVPath);
            }

            else if (Path.GetFileName(CSVPath.ToUpper()).StartsWith("STORAGE"))
            {
                isActiveSetToZero("STOREtable");
                STORECLASSToDatabase(CSVPath);
            }

            else if (Path.GetFileName(CSVPath.ToUpper()).StartsWith("PO"))
            {
                isActiveSetToZero("POtable");
                POToDatabase(CSVPath);
            }

            else
                Common.LogToFile(mLogpath, String.Format("No Update happen.Please Verify files comes with names like INVSUM or Storage or PO"), 1);


        }
        private static void INVSUMToDatabase(string CSVPath)
        {
            try
            {
                List<string[]> ParsedList = TextParser.Parse(CSVPath);
                DataTable table = ModifyHeader(CSVName.INVSUM);
                for (int i = 0; i < ParsedList.Count; i++)
                {
                    String[] arr = ParsedList[i].ToArray();
                    if (String.IsNullOrEmpty(arr[26]))
                        arr[26] = null;
                    table.Rows.Add(arr);
                }

                using (SqlBulkCopy sqlBulk = new SqlBulkCopy(mDBConnectionString))
                {
                    sqlBulk.BulkCopyTimeout = 300;
                    sqlBulk.DestinationTableName = "INVSUM";
                    sqlBulk.WriteToServer(table);
                    Common.LogToFile(mLogpath, String.Format("    1. INVSUM data saved in Table..."), 3);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("Error at {0}. Error - {1}", "INVSUMToDatabase()", ex.Message));
            }

        }
        private static void STORECLASSToDatabase(string CSVPath)
        {
            try
            {
                List<string[]> ParsedList = TextParser.Parse(CSVPath);
                DataTable table = ModifyHeader(CSVName.STORAGECLASS);
                for (int i = 0; i < ParsedList.Count; i++)
                {
                    table.Rows.Add(ParsedList[i].ToArray());
                }

                using (SqlBulkCopy sqlBulk = new SqlBulkCopy(mDBConnectionString))
                {
                    sqlBulk.BulkCopyTimeout = 300;
                    sqlBulk.DestinationTableName = "STORECLASS";
                    sqlBulk.WriteToServer(table);
                    Common.LogToFile(mLogpath, String.Format("    1. STORECLASS data saved in Table..."), 3);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("Error at {0}. Error - {1}", "STORECLASSToDatabase()", ex.Message));
            }
        }
        private static void POToDatabase(string CSVPath)
        {
            try
            {
                List<string[]> ParsedList = TextParser.Parse(CSVPath);
                DataTable table = ModifyHeader(CSVName.PO);
                for (int i = 0; i < ParsedList.Count; i++)
                {
                    table.Rows.Add(ParsedList[i].Take(ParsedList[i].Count() - 1).ToArray());
                }


                using (SqlBulkCopy sqlBulk = new SqlBulkCopy(mDBConnectionString))
                {
                    sqlBulk.BulkCopyTimeout = 300;
                    sqlBulk.DestinationTableName = "PO";
                    sqlBulk.WriteToServer(table);
                    Common.LogToFile(mLogpath, String.Format("1. PO data saved in Table..."), 3);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("Error at {0}. Error - {1}", "POToDatabase()", ex.Message));
            }


        }
        private static void isActiveSetToZero(string tableName)
        {
            try
            {
                using (SqlConnection sConn = new SqlConnection(mDBConnectionString))
                {
                    Common.LogToFile(mLogpath, String.Format("Updating records for table {0} started...", tableName), 2);

                    using (SqlCommand sComm = new SqlCommand("UPDATE " + tableName + " SET isActive=0", sConn))
                    {
                        sConn.Open();
                        sComm.CommandType = CommandType.Text;
                        sComm.ExecuteNonQuery();
                        sConn.Close();
                    }
                    Common.LogToFile(mLogpath, String.Format("isActive column of the {0} Updated to 0", tableName), 3);

                }
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("Error while updating {0} in DB. Error - {1}", tableName, ex.Message));
            }
        }
        private static DataTable ModifyHeader(Enum Type)
        {
            try
            {
                DataTable table = new DataTable();
                if (Type.ToString() == "INVSUM")
                {
                    table.Columns.Add("sd_item", typeof(String));
                    table.Columns.Add("fg_lot_num", typeof(String));
                    table.Columns.Add("hg_short_descr", typeof(String));              
                    table.Columns.Add("ee_item", typeof(Decimal));
                    table.Columns.Add("we_item", typeof(DateTime));//DateTime
                    table.Columns.Add("we_item", typeof(String));
                    table.Columns.Add("eww_item", typeof(String));
                    table.Columns.Add("ewe_item", typeof(String));
                    table.Columns.Add("ewe_item", typeof(String));
                    table.Columns.Add("eee_item", typeof(String));
                }
                else if (Type.ToString() == "PO")
                {
                    table.Columns.Add("we_num", typeof(String));
                    table.Columns.Add("ewew", typeof(String));
                    table.Columns.Add("qty_exp", typeof(int));
                    table.Columns.Add("tot_rcv", typeof(int));
                    table.Columns.Add("date_expect", typeof(DateTime));
                    table.Columns.Add("isActive", typeof(String));
                }
                else if (Type.ToString() == "STORAGECLASS")
                {
                    table.Columns.Add("ew_class", typeof(String));
                    table.Columns.Add("we_cust_num", typeof(String));
                    table.Columns.Add("ew_descr", typeof(String));
                    table.Columns.Add("Cee_Name", typeof(String));
                    table.Columns.Add("isActive", typeof(String));
                }
                table.AcceptChanges();
                return table;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("Error at {0}. Error - {1}", "ModifyHeader()", ex.Message));
            }
        }
        enum CSVName
        {
            INVSUM,
            PO,
            STORAGECLASS
        }
    }
}