using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace ProjectCSVTODB
{
    public static class TextParser
    {
        public static List<string[]> Parse(string FilePath)
        {
            List<string[]> mylist = new List<string[]>();
            DataTable dt = new DataTable();
            using (TextFieldParser parser = new TextFieldParser(FilePath))
            {
                // parser.CommentTokens = new string[] { "#" };
                parser.SetDelimiters(new string[] { "," });
                parser.HasFieldsEnclosedInQuotes = true;
                // Skip over header line.
                parser.ReadLine();
                while (!parser.EndOfData)
                {
                    mylist.Add(parser.ReadFields());
                }
            }
            return mylist;
        }
    }
}