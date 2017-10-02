using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Runtime.Caching;
using System.Text;

namespace MemoryCacheSample
{
    public class CacheSample
    {
        private const string KEY_FILE_CONTENTS = "filecontents";
        private const string KEY_SQL_CONTENTS = "sqlcontents";

        public string GetFileContents()
        {
            string filePath = ConfigurationManager.AppSettings["SQLFile"].ToString();
            filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filePath);
            string timeout = ConfigurationManager.AppSettings["CacheTimeOut"].ToString();

            ObjectCache cache = MemoryCache.Default;
            string fileContents = cache[KEY_FILE_CONTENTS] as string;

            if (fileContents == null)
            {
                CacheItemPolicy policy = new CacheItemPolicy();
                policy.AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(Convert.ToDouble(timeout));

                List<string> filePaths = new List<string>();
                filePaths.Add(filePath);
                policy.ChangeMonitors.Add(new HostFileChangeMonitor(filePaths));

                // Fetch the file contents.
                fileContents = ReadFile(filePath);

                cache.Set(KEY_FILE_CONTENTS, fileContents, policy);
            }

            return fileContents;
        }

        public DataTable GetSQLContents()
        {
            string strDBCon = ConfigurationManager.ConnectionStrings["SQLServer"].ToString();
            string sqlFilePath = ConfigurationManager.AppSettings["SQLFile"].ToString();
            string timeout = ConfigurationManager.AppSettings["CacheTimeOut"].ToString();

            ObjectCache cache = MemoryCache.Default;
            DataTable sqlContents = cache[KEY_SQL_CONTENTS] as DataTable;

            if (sqlContents == null)
            {
                CacheItemPolicy policy = new CacheItemPolicy();
                policy.AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(Convert.ToDouble(timeout));

                try
                {
                    SqlDependency.Start(strDBCon);
                    SqlDependency dep = new SqlDependency();

                    using (SqlConnection con = new SqlConnection(strDBCon))
                    {
                        string sql = ReadFile(sqlFilePath);

                        using (SqlCommand command = new SqlCommand(sql, con))
                        {
                            con.Open();

                            SqlDataAdapter adapter = new SqlDataAdapter(command);
                            adapter.Fill(sqlContents);

                            dep.AddCommandDependency(command);

                            policy.ChangeMonitors.Add(new SqlChangeMonitor(dep));
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }

                // Fetch the SQL contents.
                cache.Set(KEY_SQL_CONTENTS, sqlContents, policy);
            }

            return sqlContents;
        }

        /// <summary>
        /// SQL文を読み込む
        /// </summary>
        /// <param name="entity"></param>
        /// <returns>SQL文</returns>
        private string ReadFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new ArgumentException("指定されたSQLファイルが見つかりません。");
            }

            string contents = File.ReadAllText(filePath, Encoding.GetEncoding("UTF-8"));

            return contents;
        }
    }
}
