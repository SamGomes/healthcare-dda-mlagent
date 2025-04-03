using System;
using System.Collections.Generic;
using SimEntities;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using UnityEngine;
using Random = UnityEngine.Random;

public class AgentManager : MonoBehaviour
{
    public SimConfig Config;
    public GameObject DDAgentPrefab;
    public int numAgents;

    /// <summary>
    /// Toggles between Mr. Blue Sky (0) or The Kite (1)
    /// </summary>
    public enum GameCond{
        MrBlueSky_BaseCalcs = 0,
        TheKite_BaseCalcs = 1,
        MrBlueSky_WithFlares = 2,
        TheKite_WithFlares = 3,
        test = 4
    }
    public GameCond gameCond; 
    
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
    
    //for base calculation
    private float CondIncPerMetric(float metric,float metricW)
    {
        return metric * metricW;
    }
    
    //with flares
    private float CondIncPerMetric(float metric,float metricW,float flareMetricsW,float maxWFlare)
    {
        float baseInc = CondIncPerMetric(metric, metricW);
        return baseInc - baseInc * (1 - flareMetricsW) * maxWFlare;
    }
    
    private float CondIncMBS_baseCalcsOnly(int playedLvls, List<(int,int)> flares, int prevLvl, int currLvl, int numLvls)
    {
        float condInc = 0.0f;
        if (currLvl > 0) //transition 00 does not make sense, so do nothing to condition
        {
            
            //weights given for each metric when calculating Condition Increase
            float[] metricsW = {0.333f,0.222f,0.111f,0.222f,0.111f}; 

            int lvlTrIndex = (prevLvl * numLvls + currLvl) - 1; //transition 00 does not make sense

            float prd =  (NormalizeFromCSV(lvlTrIndex, "average_displacement1_mrbluesky.second_played_lvl") +
                        NormalizeFromCSV(lvlTrIndex, "average_displacement2_mrbluesky.second_played_lvl")) / 2.0f;
            condInc += CondIncPerMetric(prd, metricsW[0]);

            float prt = 1.0f - (NormalizeFromCSV(lvlTrIndex, "averageTimeRight.second_played_lvl") +
                                 NormalizeFromCSV(lvlTrIndex, "averageTimeLeft.second_played_lvl")) / 2.0f;
            condInc += CondIncPerMetric(prt, metricsW[1]);
            
            float sl = 1.0f - NormalizeFromCSV(lvlTrIndex, "stressLevel_delta.second_played_lvl.mrbluesky");
            condInc += CondIncPerMetric(sl, metricsW[2]);

            float hr = 1.0f - NormalizeFromCSV(lvlTrIndex, "heartRate_delta.second_played_lvl.mrbluesky");
            condInc += CondIncPerMetric(hr, metricsW[3]);
            
            float rsq = NormalizeFromCSV(lvlTrIndex, "RSQ_mrbluesky_score_delta");
            condInc += CondIncPerMetric(rsq, metricsW[4]);
        }
        return condInc;
    }
    private float CondIncTheKite_baseCalcsOnly(int playedLvls, List<(int,int)> flares, int prevLvl, int currLvl, int numLvls)
    {
        float condInc = 0.0f;
        if (currLvl > 0) //transition 00 does not make sense, so do nothing to condition
        {
            //weights given for each metric when calculating Condition Increase
            float[] metricsW = {0.4f,0.2f,0.4f}; 

            int lvlTrIndex = (prevLvl * numLvls + currLvl) - 1; //transition 00 does not make sense
            
            float sl = 1.0f - NormalizeFromCSV(lvlTrIndex, "stressLevel_delta.second_played_lvl.thekite");
            condInc += CondIncPerMetric(sl, metricsW[0]);

            float hr = 1.0f - NormalizeFromCSV(lvlTrIndex, "heartRate_delta.second_played_lvl.thekite");
            condInc += CondIncPerMetric(hr, metricsW[1]);
            
            float rsq = NormalizeFromCSV(lvlTrIndex, "RSQ_thekite_score_delta");
            condInc += CondIncPerMetric(rsq, metricsW[2]);
        }
        return condInc;
    }

    
    private float RTest(int playedLvls, List<(int,int)> flares, int prevLvl, int currLvl, int numLvls)
    {
        float condInc = 0.0f;
        if (currLvl > 0) //transition 00 does not make sense, so do nothing to condition
        {
            
            //weights given for each metric when calculating Condition Increase
            float[] metricsW = {0.333f,0.222f,0.111f,0.222f,0.111f}; 

            int lvlTrIndex = (prevLvl * numLvls + currLvl) - 1; //transition 00 does not make sense

            float prd =  (NormalizeFromCSV(lvlTrIndex, "average_displacement1_mrbluesky.second_played_lvl") +
                          NormalizeFromCSV(lvlTrIndex, "average_displacement2_mrbluesky.second_played_lvl")) / 2.0f;
            condInc += CondIncPerMetric(prd, metricsW[0]);

            float prt = 1.0f - (NormalizeFromCSV(lvlTrIndex, "averageTimeRight.second_played_lvl") +
                                NormalizeFromCSV(lvlTrIndex, "averageTimeLeft.second_played_lvl")) / 2.0f;
            condInc += CondIncPerMetric(prt, metricsW[1]);
            
            float sl = 1.0f - NormalizeFromCSV(lvlTrIndex, "stressLevel_delta.second_played_lvl.mrbluesky");
            condInc += CondIncPerMetric(sl, metricsW[2]);

            float hr = 1.0f - NormalizeFromCSV(lvlTrIndex, "heartRate_delta.second_played_lvl.mrbluesky");
            condInc += CondIncPerMetric(hr, metricsW[3]);
            
            float rsq = NormalizeFromCSV(lvlTrIndex, "RSQ_mrbluesky_score_delta");
            condInc += CondIncPerMetric(rsq, metricsW[4]);
        }
        return condInc;
    }
    
    // This is the Condition Increase function (reward function) for a patient when playing Mr. Blue Sky 
    private float CondIncMBS(int playedLvls, List<(int,int)> flares, int prevLvl, int currLvl, int numLvls)
    {
        float condInc = 0.0f;
        if (currLvl > 0) //transition 00 does not make sense, so do nothing to condition
        {
            
            // List<float> xtest = new List<float>(10);
            // for (int i = 0; i < 10; i++) xtest.Add(0);
            // for (int i = 0; i < 10; i++)
            // {
            //     float maxWFlarel = 0;
            //     foreach (var flare in flares)
            //     {
            //         float currWFlare = GetWFlare(flare.Item1, flare.Item2, i);
            //         maxWFlarel = (maxWFlarel < currWFlare) ? currWFlare : maxWFlarel;
            //     }
            //     
            //     xtest[i] = maxWFlarel;
            // }
            // Debug.Log(xtest);
            
            float maxWFlare = 0.0f;
            foreach (var flare in flares)
            {
                float currWFlare = GetWFlare(flare.Item1, flare.Item2, playedLvls);
                maxWFlare = (maxWFlare < currWFlare) ? currWFlare : maxWFlare;
            }

            //weights given for each metric when calculating Condition Increase
            float[] metricsW = {0.333f,0.222f,0.111f,0.222f,0.111f}; 
            
            //importance of each metric when changing Condition Increase on flare
            float[] flareMetricsW = {0.5f,0.5f,2.0f,1.0f,2.0f};

            int lvlTrIndex = (prevLvl * numLvls + currLvl) - 1; //transition 00 does not make sense

            float prd =  (NormalizeFromCSV(lvlTrIndex, "average_displacement1_mrbluesky.second_played_lvl") +
                        NormalizeFromCSV(lvlTrIndex, "average_displacement2_mrbluesky.second_played_lvl")) / 2.0f;
            condInc += CondIncPerMetric(prd, metricsW[0], flareMetricsW[0], maxWFlare);

            float prt = 1.0f - (NormalizeFromCSV(lvlTrIndex, "averageTimeRight.second_played_lvl") +
                                 NormalizeFromCSV(lvlTrIndex, "averageTimeLeft.second_played_lvl")) / 2.0f;
            condInc += CondIncPerMetric(prt, metricsW[1], flareMetricsW[1], maxWFlare);
            
            float sl = 1.0f - NormalizeFromCSV(lvlTrIndex, "stressLevel_delta.second_played_lvl.mrbluesky");
            condInc += CondIncPerMetric(sl, metricsW[2], flareMetricsW[2], maxWFlare);

            float hr = 1.0f - NormalizeFromCSV(lvlTrIndex, "heartRate_delta.second_played_lvl.mrbluesky");
            condInc += CondIncPerMetric(hr, metricsW[3], flareMetricsW[3], maxWFlare);
            
            float rsq = NormalizeFromCSV(lvlTrIndex, "RSQ_mrbluesky_score_delta");
            condInc += CondIncPerMetric(rsq, metricsW[4], flareMetricsW[4], maxWFlare);
        }
        return condInc;
    }
    private float CondIncTheKite(int playedLvls, List<(int,int)> flares, int prevLvl, int currLvl, int numLvls)
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
            float[] metricsW = {0.4f,0.2f,0.4f}; 
            
            //importance of each metric when changing Condition Increase on flare
            float[] flareMetricsW = {2.0f,1.0f,2.0f};

            int lvlTrIndex = (prevLvl * numLvls + currLvl) - 1; //transition 00 does not make sense
            
            float sl = 1.0f - NormalizeFromCSV(lvlTrIndex, "stressLevel_delta.second_played_lvl.thekite");
            condInc += CondIncPerMetric(sl, metricsW[0], flareMetricsW[0], maxWFlare);

            float hr = 1.0f - NormalizeFromCSV(lvlTrIndex, "heartRate_delta.second_played_lvl.thekite");
            condInc += CondIncPerMetric(hr, metricsW[1], flareMetricsW[1], maxWFlare);
            
            float rsq = NormalizeFromCSV(lvlTrIndex, "RSQ_thekite_score_delta");
            condInc += CondIncPerMetric(rsq, metricsW[2], flareMetricsW[2], maxWFlare);
        }
        return condInc;
    }
        
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {

        switch (gameCond)
        {
            case GameCond.MrBlueSky_BaseCalcs:
                Config = new SimConfig(4,
                    3,
                    new List<string>() { "A", "B", "C" },
                    "ExpData/processed_data_mrbluesky_bytransition",
                    CondIncMBS_baseCalcsOnly,
                    0);
                break;
                
            case GameCond.MrBlueSky_WithFlares:
                Config = new SimConfig(4,
                    3,
                    new List<string>() { "A", "B", "C" },
                    "ExpData/processed_data_mrbluesky_bytransition",
                    CondIncMBS,
                    84);
                break;
            case GameCond.TheKite_BaseCalcs:
                Config = new SimConfig(4,
                    4,
                    new List<string>() { "A", "B", "C", "D" },
                    "ExpData/processed_data_thekite_bytransition",
                    CondIncTheKite_baseCalcsOnly,
                    0);
                break;
                
            case GameCond.TheKite_WithFlares:
                Config = new SimConfig(4,
                    4,
                    new List<string>() { "A", "B", "C", "D" },
                    "ExpData/processed_data_thekite_bytransition",
                    CondIncTheKite,
                    84);
                break;
            
            case GameCond.test:
                Config = new SimConfig(4,
                    4,
                    new List<string>() { "A", "B", "C", "D" },
                    "ExpData/processed_data_thekite_bytransition",
                    RTest,
                    0);
                break;
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
