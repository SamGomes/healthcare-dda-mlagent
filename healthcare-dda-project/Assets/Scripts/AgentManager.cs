using System;
using System.Collections.Generic;
using SimEntities;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using UnityEngine;

public class AgentManager : MonoBehaviour
{
    public SimConfig Config;
    public GameObject DDAgentPrefab;
    public int numAgents = 0;
    
    
    private float NormalizeFromCSV(int lvlTrIndex, string valueCSVAttr)
    {
        float value = Convert.ToSingle(Config.Measures[lvlTrIndex][valueCSVAttr]); // cast to float did not work for some reason
        float min = Convert.ToSingle(Config.Measures[lvlTrIndex]["min."+valueCSVAttr]);
        float max = Convert.ToSingle(Config.Measures[lvlTrIndex]["max."+valueCSVAttr]);
        return (value - min) / (max - min);
    }

    private float CondIncMBS(int prevLvl, int currLvl, int numLvls)
    {
        float condInc = 0.0f;
        if (currLvl > 0) //transition 00 does not make sense, so do nothing to condition
        {
            int lvlTrIndex = (prevLvl * numLvls + currLvl) - 1; //transition 00 does not make sense
            
            condInc += NormalizeFromCSV(lvlTrIndex, "averageTimeRight.second_played_lvl");
            condInc += NormalizeFromCSV(lvlTrIndex, "averageTimeLeft.second_played_lvl");
            condInc += 1.0f - NormalizeFromCSV(lvlTrIndex, "stressLevel_delta.second_played_lvl.mrbluesky");
            condInc += 1.0f - NormalizeFromCSV(lvlTrIndex, "heartRate_delta.second_played_lvl.mrbluesky");
            condInc += NormalizeFromCSV(lvlTrIndex, "average_displacement1_mrbluesky.second_played_lvl");
            condInc += NormalizeFromCSV(lvlTrIndex, "average_displacement2_mrbluesky.second_played_lvl");
            condInc += NormalizeFromCSV(lvlTrIndex, "RSQ_mrbluesky_score_delta");
            condInc /= 7.0f;
        }
        return condInc;
    }
    private float CondIncTheKite(int prevLvl, int currLvl, int numLvls)
    {
        float condInc = 0.0f;
        if (currLvl > 0) //transition 00 does not make sense, so do nothing to condition
        {
            int lvlTrIndex = (prevLvl * numLvls + currLvl) - 1; //transition 00 does not make sense
            condInc += 1.0f - NormalizeFromCSV(lvlTrIndex,"stressLevel_delta.second_played_lvl.thekite");
            condInc += 1.0f - NormalizeFromCSV(lvlTrIndex,"heartRate_delta.second_played_lvl.thekite");
            condInc += NormalizeFromCSV(lvlTrIndex,"RSQ_thekite_score_delta");
            condInc /= 3.0f;
        }
        return condInc;
    }
        
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        
        // for Mr. Blue Sky
        // config = new SimConfig(3,
        //      new List<string>() {"A", "B", "C"},
        //     "ExpData/processed_data_mrbluesky_bytransition",
        //     CondIncMBS);

        
        // for The Kite
        Config = new SimConfig(4,
            new List<string>() {"A", "B", "C", "D"},
            "ExpData/processed_data_thekite_bytransition",
            CondIncTheKite);

        BehaviorParameters behaviorParameters = DDAgentPrefab.GetComponent<BehaviorParameters>();
        behaviorParameters.BrainParameters.ActionSpec =
            ActionSpec.MakeDiscrete(Config.NumLvls + 1);
        behaviorParameters.BrainParameters.OnBeforeSerialize();
        for (int i = 0; i < numAgents; i++)
        {
            Instantiate(DDAgentPrefab,GameObject.Find("Agents").transform);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
