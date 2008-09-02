using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace TowseyLib
{
    public class Configuration
    {
        private string fName;
        Hashtable table;

        public Configuration()
        {
            this.table = new Hashtable();
        }

        public Configuration(string fName)
        {
            this.fName = fName;
            this.table = FileTools.ReadPropertiesFile(fName);
        }

        public bool ContainsKey(string key)
        {
            return this.table.ContainsKey(key);
        }

        public string GetString(string key)
        {
            bool keyExists = this.table.ContainsKey(key);
            if (keyExists) return this.table[key].ToString();
            else return null;
        }
        public int GetInt(string key)
        {
            bool keyExists = this.table.ContainsKey(key);
            if (!keyExists) return -Int32.MaxValue;
            string value = this.table[key].ToString();
            if (value == null) return -Int32.MaxValue;
            int int32;
            try
            {
                int32 = Int32.Parse(value);
            }
            catch(System.FormatException ex)
            {
                System.Console.WriteLine("ERROR READING PROPERTIES FILE");
                System.Console.WriteLine("INVALID VALUE=" + value);
                System.Console.WriteLine(ex);
                return Int32.MaxValue;
            }
                return int32;
        }
        public double GetDouble(string key)
        {
            bool keyExists = this.table.ContainsKey(key);
            if (!keyExists) return -Double.MaxValue;
            string value = this.table[key].ToString();
            if (value == null) return -Double.MaxValue;
            double d;
            try
            {
                d = Double.Parse(value);
            }
            catch (System.FormatException ex)
            {
                System.Console.WriteLine("ERROR READING PROPERTIES FILE");
                System.Console.WriteLine("INVALID VALUE=" + value);
                System.Console.WriteLine(ex);
                return Double.MaxValue;
            }
            return d;
        }

        public bool GetBoolean(string key)
        {
            bool keyExists = this.table.ContainsKey(key);
            if (!keyExists) return false;
            bool b = false;
            string value = this.table[key].ToString();
            if (value == null) return b;
            try
            {
                b = Boolean.Parse(value);
            }
            catch (System.FormatException ex)
            {
                System.Console.WriteLine("ERROR READING PROPERTIES FILE");
                System.Console.WriteLine("INVALID VALUE=" + value);
                System.Console.WriteLine(ex);
                return false;
            }
            return b;
        }//end getBoolean



    }  // end of class Configuration
}
