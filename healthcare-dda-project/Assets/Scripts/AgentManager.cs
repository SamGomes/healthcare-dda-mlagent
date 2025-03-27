using System;
using System.Collections.Generic;
using SimEntities;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using UnityEngine;
using UnityEngine.InputSystem.Controls;

public class AgentManager : MonoBehaviour
{
    public SimConfig Config;
    public GameObject DDAgentPrefab;
    public int numAgents;
    
    /// <summary>
    /// Toggles between Mr. Blue Sky (0) or The Kite (1)
    /// </summary>
    public int gameId = 0; 
    
    private float NormalizeFromCSV(int lvlTrIndex, string valueCSVAttr)
    {
        float value = Convert.ToSingle(Config.Measures[lvlTrIndex][valueCSVAttr]); // cast to float did not work for some reason
        float min = Convert.ToSingle(Config.Measures[lvlTrIndex]["min."+valueCSVAttr]);
        float max = Convert.ToSingle(Config.Measures[lvlTrIndex]["max."+valueCSVAttr]);
        return (value - min) / (max - min);
    }

    private float GetWFlare(int mean, int var, int t)
    {
        if (t > mean + 0.5f*var || t < mean - 0.5f*var)
        {
            return 0.0f;
        }

        float tf = t - (mean - 0.5f*var);
        tf /= var;
        return -0.5f * (float) Math.Cos(2 * Mathf.PI * tf) + 0.5f;
    }


    // This is the Condition Increase function (reward function) for a patient when playing Mr. Blue Sky 
    private float CondIncMBS(int playedLvls, List<(int,int)> flares, int prevLvl, int currLvl, int numLvls)
    {
        float condInc = 0.0f;
        if (currLvl > 0) //transition 00 does not make sense, so do nothing to condition
        {
            float maxWFlare = 0;
            foreach (var flare in flares)
            {
                float currWFlare = GetWFlare(flare.Item1, flare.Item2, playedLvls);
                maxWFlare = (maxWFlare < currWFlare) ? currWFlare : maxWFlare;
            }

            //weights given for each metric when calculating Condition Increase
            float[] metricsW = new float[]{0.333f,0.222f,0.111f,0.222f,0.111f}; 
            
            //importance of each metric when changing Condition Increase on flare
            float[] flareMetricsW = new float[]{0.3f,0.3f,1.0f,0.5f,1.0f}; 

            int lvlTrIndex = (prevLvl * numLvls + currLvl) - 1; //transition 00 does not make sense

            // prd
            float prd =  (NormalizeFromCSV(lvlTrIndex, "average_displacement1_mrbluesky.second_played_lvl") +
                        NormalizeFromCSV(lvlTrIndex, "average_displacement2_mrbluesky.second_played_lvl")) / 2.0f;
            condInc += prd * metricsW[0];
            condInc += condInc * prd * maxWFlare * flareMetricsW[0];

            // prt
            float prt =  (NormalizeFromCSV(lvlTrIndex, "averageTimeRight.second_played_lvl") +
                         NormalizeFromCSV(lvlTrIndex, "averageTimeLeft.second_played_lvl")) / 2.0f;
            condInc += prt * metricsW[1];
            condInc += condInc * prt * maxWFlare * flareMetricsW[1];
            
            float sl = 1.0f - NormalizeFromCSV(lvlTrIndex, "stressLevel_delta.second_played_lvl.mrbluesky");
            condInc += sl * metricsW[2];
            condInc += condInc * sl * maxWFlare * flareMetricsW[2];

            float hr = 1.0f - NormalizeFromCSV(lvlTrIndex, "heartRate_delta.second_played_lvl.mrbluesky");
            condInc += hr * metricsW[3];
            condInc += condInc * hr * maxWFlare * flareMetricsW[3];
            
            float rsq = NormalizeFromCSV(lvlTrIndex, "RSQ_mrbluesky_score_delta");
            condInc += rsq * metricsW[4];
            condInc += condInc * rsq * maxWFlare * flareMetricsW[4];

            // condInc += NormalizeFromCSV(lvlTrIndex, "averageTimeRight.second_played_lvl");
            // condInc += NormalizeFromCSV(lvlTrIndex, "averageTimeLeft.second_played_lvl");
            // condInc += 1.0f - NormalizeFromCSV(lvlTrIndex, "stressLevel_delta.second_played_lvl.mrbluesky");
            // condInc += 1.0f - NormalizeFromCSV(lvlTrIndex, "heartRate_delta.second_played_lvl.mrbluesky");
            // condInc += NormalizeFromCSV(lvlTrIndex, "average_displacement1_mrbluesky.second_played_lvl");
            // condInc += NormalizeFromCSV(lvlTrIndex, "average_displacement2_mrbluesky.second_played_lvl");
            // condInc += NormalizeFromCSV(lvlTrIndex, "RSQ_mrbluesky_score_delta");
            // condInc /= 7.0f;
            return condInc;
        }
        return condInc;
    }
    private float CondIncTheKite(int playedLvls, List<(int,int)> flares, int prevLvl, int currLvl, int numLvls)
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

        if (gameId == 0)
        {
            // for Mr. Blue Sky
            Config = new SimConfig(120,
                3,
                 new List<string>() {"A", "B", "C"},
                "ExpData/processed_data_mrbluesky_bytransition",
                CondIncMBS);
        }
        else
        {
            // for The Kite
            Config = new SimConfig(120,
                4,
                new List<string>() { "A", "B", "C", "D" },
                "ExpData/processed_data_thekite_bytransition",
                CondIncTheKite);
        }

        BehaviorParameters behaviorParameters = DDAgentPrefab.GetComponent<BehaviorParameters>();
        behaviorParameters.BrainParameters.ActionSpec =
            ActionSpec.MakeDiscrete(Config.NumGameLvls + 1);
        behaviorParameters.BrainParameters.OnBeforeSerialize();
        for (int i = 0; i < numAgents; i++)
        {
            Instantiate(DDAgentPrefab,GameObject.Find("Agents").transform);
        }
    }
}
