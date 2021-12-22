using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using csvToCityJSON.csv;


namespace csvToCityJSON
{
    class csvTocityJsonConverter
    {
        public void start()
        {
            showUniqueValues("Categorie","D:/trees/brondata");
            showUniqueValues("Boomhoogte", "D:/trees/brondata");
            // read config-file

            // read csv
            //ReadCSVFiles("D:/trees/brondata");
            // bepaal geometrie-sjabloon

            // bepaal locatie

            // locatie omrekenen wgs84 -> RD

            // read obj-files

            // create cityJSON geometryTemplates

            // create cityJSON cityObjects


        }

        public void showUniqueValues(string property, string sourcePath)
        {
            string[] sourceFiles = Directory.GetFiles(sourcePath, "*.csv");
            if (sourceFiles.Length == 0)
            {
                Console.WriteLine($"No csv-files found in {sourcePath}");
                return;
            }
            csv.csvParse csvParser = new csv.csvParse();
            for (int i = 0; i < sourceFiles.Length; i++)
            {
                csvParser.getUniquePropertyValues(property,sourceFiles[i]);
            }

            Console.WriteLine($"property {property} contains:");
            for (int i = 0; i < csvParser.propertyValues.Count; i++)
            {
                Console.WriteLine(csvParser.propertyValues[i]);
            }
            
        }

        public void ReadCSVFiles(string sourcePath)
        {
            //List the files that we are going to parse
            string[] sourceFiles = Directory.GetFiles(sourcePath, "*.csv");
            if (sourceFiles.Length == 0)
            {
                Console.WriteLine($"No csv-files found in {sourcePath}");
                return;
            }
            csv.csvParse csvParser= new csv.csvParse();
            for (int i = 0; i < sourceFiles.Length; i++)
            {
                csvParser.parseFile(sourceFiles[i]);
            }
            Console.WriteLine(csvParser.csvItems.Count);
        }
    }
}
