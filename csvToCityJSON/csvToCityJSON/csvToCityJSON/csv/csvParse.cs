using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace csvToCityJSON.csv
{
    class csvParse
    {
        public List<csvItem> csvItems;
        public List<string> propertyValues;

        public csvParse()
        {
            csvItems = new List<csvItem>();
            propertyValues =new List<string>(); ;
        }

        public void getUniquePropertyValues(string property,string fileName)
        {
            string[] columnNames;
            string[] itemdata;
            int searchID = 0;
            using (StreamReader streamReader = File.OpenText(fileName))
            {
                string Line = streamReader.ReadLine();
                columnNames = Line.Split(";");
                for (int i = 0; i < columnNames.Length; i++)
                {
                    if (columnNames[i]==property)
                    {
                        searchID = i;
                    }
                }
                while (!streamReader.EndOfStream)
                {
                    Line = streamReader.ReadLine();
                    itemdata = Line.Split(";");
                    if (itemdata.Length != columnNames.Length)
                    {
                        Console.WriteLine("found an invalid line");
                        continue;
                    }
                    if (!propertyValues.Contains(itemdata[searchID]))
                    {
                        propertyValues.Add(itemdata[searchID]);
                    }
                }
            }

        }

        public void parseFile(string fileName)
        {
            string[] columnNames;
            string[] itemdata;
            using (StreamReader streamReader = File.OpenText(fileName))
            {
                string Line = streamReader.ReadLine();
                columnNames =Line.Split(";");
                while (!streamReader.EndOfStream)
                {
                    Line = streamReader.ReadLine();
                    itemdata = Line.Split(";");
                    if (itemdata.Length!=columnNames.Length)
                    {
                        Console.WriteLine("found an invalid line");
                        continue;
                    }
                    csvItem csvItem = new csvItem();
                    Dictionary<string, string> itemDict = new Dictionary<string, string>();
                    for (int i = 0; i < itemdata.Length; i++)
                    {
                        itemDict.Add(columnNames[i], itemdata[i]);
                    }
                    csvItem.properties = itemDict;
                    csvItems.Add(csvItem);
                }
            }
        }
    }
}
