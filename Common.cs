using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ProjectCSVTODB
{
    public static class Common
    {
        #region LogToFile

        public static void LogToFile(string path, string txt, int code)
        {
            //ConsoleSpiner spin = new ConsoleSpiner();

            if (!string.IsNullOrEmpty(path))
            {
                path = Path.Combine(path, String.Format("LoadDCWReport_{0}.txt", DateTime.Now.ToString("yyyyMMdd")));

                //--- Write message to existing Log file
                System.IO.StreamWriter w = null;
                try
                {
                    string logDir = Path.GetDirectoryName(path);
                    if (!Directory.Exists(logDir))
                        Directory.CreateDirectory(logDir);

                    // open file
                    System.IO.FileInfo f = new FileInfo(path);
                    w = f.AppendText();
                    // write a line
                    w.WriteLine("{0}\t{1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), txt);
                    // Update the underlying file.
                    w.Flush();

                    f = null;
                    string color = (code == 1 ? "White" : (code == 2 ? "Yellow" : (code == 3 ? "Green" : "Red")));
                    Console.WriteLine("<f=" + color + ">" + txt);
                    //Console.Write("Working....");
                    //while (true)
                    //{
                    //    spin.Turn();
                    //}
                }
                catch (Exception ex)
                {
                    throw new Exception("LogToFile fail. " + ex.Message);
                }
                finally
                {
                    // Close the writer and underlying file.
                    if (w != null)
                        w.Close();

                    logFileCleanup(path);
                }
            }
        }

        private static void logFileCleanup(string path)
        {
            try
            {

                // open file
                System.IO.FileInfo f = new FileInfo(path);

                if (f.Length > 5 * 1024 * 1024)
                {
                    f.CopyTo(path.Replace(f.Extension, "_" + DateTime.Now.ToString("yyyyMMddHHmmss")) + f.Extension);
                    f.Delete();
                }
                f = null;
            }
            catch (Exception ex)
            {
                throw new Exception("LogToFile fail. " + ex.Message);
            }
        }
        #endregion
        #region Console
        public static class Console
        {
            private static Regex _writeRegex = new Regex("<[fb]=\\w+>");
            public static void WriteLine(string value, int? cursorPosition = null, bool clearRestOfLine = false)
            {
                Write(value + Environment.NewLine, cursorPosition, clearRestOfLine);
            }
            public static void Write(string value, int? cursorPosition = null, bool clearRestOfLine = false)
            {
                if (cursorPosition.HasValue)
                    System.Console.CursorLeft = cursorPosition.Value;
                ConsoleColor defaultForegroundColor = System.Console.ForegroundColor;
                ConsoleColor defaultBackgroundColor = System.Console.BackgroundColor;
                var segments = _writeRegex.Split(value);
                var colors = _writeRegex.Matches(value);
                for (int i = 0; i < segments.Length; i++)
                {
                    if (i > 0)
                    {
                        ConsoleColor consoleColor;
                        // Now that we have the color tag, split it int two parts, 
                        // the target(foreground/background) and the color.
                        var splits = colors[i - 1].Value
                            .Trim(new char[] { '<', '>' })
                            .Split('=')
                            .Select(str => str.ToLower().Trim())
                            .ToArray();
                        // if the color is set to d (default), then depending on our target,
                        // set the color to be the default for that target.
                        if (splits[1] == "d")
                            if (splits[0][0] == 'b')
                                consoleColor = defaultBackgroundColor;
                            else
                                consoleColor = defaultForegroundColor;
                        else
                            // Grab the console color that matches the name passed. 
                            // If none match, then return default (black).
                            consoleColor = Enum.GetValues(typeof(ConsoleColor))
                                .Cast<ConsoleColor>()
                                .FirstOrDefault(en => en.ToString().ToLower() == splits[1]);
                        // Set the now chosen color to the specified target.
                        if (splits[0][0] == 'b')
                            System.Console.BackgroundColor = consoleColor;
                        else
                            System.Console.ForegroundColor = consoleColor;
                    }
                    // Only bother writing out, if we have something to write.
                    if (segments[i].Length > 0)
                        System.Console.Write(segments[i]);
                }
                System.Console.ForegroundColor = defaultForegroundColor;
                System.Console.BackgroundColor = defaultBackgroundColor;
                if (clearRestOfLine)
                    ClearRestOfLine();
            }
            public static void ClearRestOfLine()
            {
                int winTop = System.Console.WindowTop;
                int left = System.Console.CursorLeft;
                System.Console.Write(new string(' ', System.Console.WindowWidth - left));
                System.Console.CursorLeft = left;
                System.Console.CursorTop--;
                System.Console.WindowTop = winTop;
            }
        }
        #endregion
        #region sendEmail
        public static void sendEmail(string sender, string recepient, string SMTPServer, string emailSubject, string emailMessage)
        {
            try
            {
                string separator = ",";
                string[] recepients = recepient.Split(separator[0]);
                //--- recepient(s)
                string smtpServer = Convert.ToString(SMTPServer); ;
                SmtpClient smtpClient = new SmtpClient();
                MailMessage message = new MailMessage();
                MailAddress fromAddress = new MailAddress(sender);
                message.From = fromAddress;
                smtpClient.Host = smtpServer;
                foreach (string emailto in recepients)
                    message.To.Add(emailto);
                message.Subject = emailSubject;
                message.Body = emailMessage;
                smtpClient.Send(message);
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("Error sending email - {0}", ex.Message));
            }
        }
        #endregion
    }
}