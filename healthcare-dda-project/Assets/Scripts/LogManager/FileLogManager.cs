using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

//File log manager
public class FileLogManager : LogManager
{
    public override void InitLogs(MonoBehaviour monoBehaviourObject)
    {
        return;
    }
    
    public override IEnumerator WriteToLog(string database, string table,
        Dictionary<string, string> argsNValues, bool justHeaders) {
        string path = Application.streamingAssetsPath 
                      + "/Log/"+table+".csv";
        
        string strToWrite = "";

        
        if (!File.Exists(path))
        {
            strToWrite += StringifyDictionaryForCSVLogs(argsNValues,true)+"\n";
        }

        if (!justHeaders)
        {
            strToWrite += StringifyDictionaryForCSVLogs(argsNValues, false);
        }
        
        StreamWriter writer = new StreamWriter(path, true);
        writer.WriteLine(strToWrite);
        writer.Close();
        
        yield return null;
    }


    public override IEnumerator GetFromLog(string database, string table, string query, Func<string, int> yieldedReactionToGet) {
        Debug.Log("database: " + database + " ; " + table + " ; ");
        yield return yieldedReactionToGet("[]");
    }

    public override IEnumerator UpdateLog(string database, string table, string query, Dictionary<string, string> argsNValues)
    {
        Debug.Log("database: " + database + " ; " + table + " ; ");
        yield return null;
    }

    public override IEnumerator EndLogs()
    {
        Debug.Log("Log Closed.");
        yield return null;
    }
}