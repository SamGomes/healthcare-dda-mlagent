using System;
using System.Collections.Generic;
using SimEntities;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using UnityEngine;
using UnityEngine.Serialization;

public class AgentManager : MonoBehaviour
{
    public SimConfig Config;
    public GameObject DDAgentPrefab;
    public int numAgents;

    public enum GameCond{
        MrBlueSky_BaseCalcs = 0,
        TheKite_BaseCalcs = 1,
        MrBlueSky_WithFlares = 2,
        TheKite_WithFlares = 3
    }
    
    public enum FlareModCond{
        Normal = 0,
        OnOff = 1
    }
    
    public enum BehaviorCond{
        RealExperiments = 0,
        StaticProfile = 1,
        DynamicProfile = 2
    }
    
    public GameCond gameCond;
    public FlareModCond flareModCond;
    public BehaviorCond behaviorCond;
    public string algName;
    
    private float NormalizeFromCSV(int lvlTrIndex, string valueCSVAttr)
    {
        float value = Convert.ToSingle(Config.Measures[lvlTrIndex][valueCSVAttr]); // cast to float did not work for some reason
        float min = Convert.ToSingle(Config.Measures[lvlTrIndex]["min."+valueCSVAttr]);
        float max = Convert.ToSingle(Config.Measures[lvlTrIndex]["max."+valueCSVAttr]);
        float maxminusmin = max - min;
        return maxminusmin == 0? min: (value - min) / maxminusmin;
    }

    private float GetWFlare(int mean, int var, int t)
    {
        
        if (t > mean + 0.5f*var || t < mean - 0.5f*var)
        {
            return 0.0f;
        }
        
        float tf = t - (mean - 0.5f*var);
        tf /= var;
        if (Config.FlareModCond == FlareModCond.Normal)
        {
            return -0.5f * (float)Math.Cos(2 * Mathf.PI * tf) + 0.5f;
        } //if (Config.FlareModCond == FlareModCond.Extremes)
        return (tf < 0.33334f || tf > 0.66667f)? 0.0f : 1.0f;
        
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
    
    // This is the Condition Increase function (reward function) for a patient when playing Mr. Blue Sky 
    private float CondIncMBS(int playedLvls, List<(int,int)> flares, int prevLvl, int currLvl, int numLvls)
    {
        float condInc = 0.0f;
        if (currLvl > 0) //transition 00 does not make sense, so do nothing to condition
        {
            
            // List<float> xtest = new List<float>(100);
            // List<float> xtest2 = new List<float>(100);
            // for (int i = 0; i < 100; i++)
            // {
            //     xtest.Add(0);
            //     float maxWFlarel = 0;
            //     Config.FlareModCond = FlareModCond.Normal;
            //     foreach (var flare in flares)
            //     {
            //         float currWFlare = GetWFlare(flare.Item1, flare.Item2, i);
            //         maxWFlarel = (maxWFlarel < currWFlare) ? currWFlare : maxWFlarel;
            //     }
            //     
            //     xtest[i] = maxWFlarel;
            //     
            //     xtest2.Add(0);
            //     maxWFlarel = 0;
            //     Config.FlareModCond = FlareModCond.OnOff;
            //     foreach (var flare in flares)
            //     {
            //         float currWFlare = GetWFlare(flare.Item1, flare.Item2, i);
            //         maxWFlarel = (maxWFlarel < currWFlare) ? currWFlare : maxWFlarel;
            //     }
            //     
            //     xtest2[i] = maxWFlarel;
            // }
            // Debug.Log(xtest);
            // Debug.Log(xtest2);
            
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

        string[] behaviorCond_MBS =
            new[]{
                new[]{"ExpData/processed_data_mrbluesky_bytransition"},
                new[]{"ExpData/HypotheticCases/Profile1/processed_data_mrbluesky_bytransition_profile1"},
                new[]{
                    "ExpData/processed_data_mrbluesky_bytransition",
                    "ExpData/HypotheticCases/Profile1/processed_data_mrbluesky_bytransition_profile1"
                }
            }[(int)behaviorCond];
        string[] behaviorCond_TK =
            new[]{
                new[]{"ExpData/processed_data_thekite_bytransition"},
                new[]{"ExpData/HypotheticCases/Profile1/processed_data_thekite_bytransition_profile1"},
                new[]
                {
                    "ExpData/processed_data_thekite_bytransition",
                    "ExpData/HypotheticCases/Profile1/processed_data_thekite_bytransition_profile1"
                }
            }[(int)behaviorCond];
        switch (gameCond)
        {
            case GameCond.MrBlueSky_BaseCalcs:
                Config = new SimConfig(
                    algName,
                    gameCond.ToString(),
                    flareModCond,
                    4,
                    3,
                    new List<string>() { "A", "B", "C" },
                    behaviorCond_MBS,
                    CondIncMBS_baseCalcsOnly,
                    0);
                break;
                
            case GameCond.MrBlueSky_WithFlares:
                Config = new SimConfig(
                    algName,
                    gameCond.ToString(),
                    flareModCond,
                    4,
                    3,
                    new List<string>() { "A", "B", "C" },
                    behaviorCond_MBS,
                    CondIncMBS,
                    84);
                break;
            case GameCond.TheKite_BaseCalcs:
                Config = new SimConfig(
                    algName,
                    gameCond.ToString(),
                    flareModCond,
                    4,
                    4,
                    new List<string>() { "A", "B", "C", "D" },
                    behaviorCond_TK,
                    CondIncTheKite_baseCalcsOnly,
                    0);
                break;
                
            case GameCond.TheKite_WithFlares:
                Config = new SimConfig(
                    algName,
                    gameCond.ToString(),
                    flareModCond,
                    4,
                    4,
                    new List<string>() { "A", "B", "C", "D" },
                    behaviorCond_TK,
                    CondIncTheKite,
                    84);
                break;
            
            // case GameCond.test:
            //     Config = new SimConfig(
            //         algName,
            //         gameCond.ToString(),
            //         flareModCond,
            //         4,
            //         4,
            //         new List<string>() { "A", "B", "C", "D" },
            //         behaviorCond_TK,
            //         RTest,
            //         0);
            //     break;
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
