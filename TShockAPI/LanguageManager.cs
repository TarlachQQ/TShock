using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Newtonsoft.Json;

namespace TShockAPI
{
    class LanguageManager
    {
        public static System.Collections.Generic.Dictionary<string, string> LanguageStrings = new System.Collections.Generic.Dictionary<string, string>();

        public static LanguageFile loadedStrings;

        public static void LoadLanguageStrings()
        {
            TextReader tr = new StreamReader(ConfigurationManager.LanguageFile);
            loadedStrings = JsonConvert.DeserializeObject<LanguageFile>(tr.ReadToEnd());
            tr.Close();
        }
    }
}
