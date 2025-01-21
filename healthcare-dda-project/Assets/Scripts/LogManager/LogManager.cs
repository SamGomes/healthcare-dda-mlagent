using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public abstract class LogManager
{
    public string StringifyDictionaryForJsonLogs(Dictionary<string,string> dict)
    {
        string result = "{";
        List<string> dictKeys = new List<string>(dict.Keys);
        for (int keyI=0; keyI < dictKeys.Count; keyI++)
        {
            string key = dictKeys[keyI];

            result += " "+ key + ": \"" + dict[key] + "\"";
            if(keyI < dictKeys.Count - 1)
            {
                result += ",";
            }
            else
            {
                result += " }";

            }
        }
        return result;
    }

    public string StringifyDictionaryForCSVLogs(Dictionary<string,string> dict, bool isHeader)
    {
        string result = "";
        List<string> dictKeys = new List<string>(dict.Keys);
        for (int keyI=0; keyI < dictKeys.Count; keyI++)
        {
            string key = dictKeys[keyI];

            if (isHeader)
            {
                result += key;
            }
            else
            {
                result += dict[key];
            }
            if(keyI < dictKeys.Count - 1)
            {
                result += ",";
            }
        }
        return result;
    }

    public abstract void InitLogs(MonoBehaviour monoBehaviourObject);
    public abstract IEnumerator WriteToLog(string database, string table, Dictionary<string, string> argsNValues, bool justHeaders);
   
    public abstract IEnumerator GetFromLog(string database, string table, string query, Func<string, int> yieldedReactionToGet);
    public abstract IEnumerator UpdateLog(string database, string table, string query, Dictionary<string, string> argsNValues);
    public abstract IEnumerator EndLogs();
}
