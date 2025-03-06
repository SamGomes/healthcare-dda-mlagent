using System;
using System.Collections.Generic;
using SimEntities;
using TMPro;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using Unity.VisualScripting;
using UnityEngine.UI;



public class DDAAgent : Agent
{
    private SimConfig config;
    private GameWrapper game;
    private PatientWrapper patient;

    public bool initUI;
    
    public GameObject gameUI;
    
    public GameObject freqHeatmap;
    public GameObject freqHeatmapSquarePrefab;
    private List<Image> freqHeatmapCells;
    public GameObject freqHeatmapAxisMarksX;
    public GameObject freqHeatmapAxisMarksY;
    public GameObject freqHeatmapAxisMarkPrefab;
    
    private List<int> currDDAStrat;
    private float pInc;
    
    private int numCellsPerDim;
    
    private float NormalizeFromCSV(int lvlTrIndex, string valueCSVAttr)
    {
        float value = Convert.ToSingle(config.Measures[lvlTrIndex][valueCSVAttr]); // cast to float did not work for some reason
        float min = Convert.ToSingle(config.Measures[lvlTrIndex]["min."+valueCSVAttr]);
        float max = Convert.ToSingle(config.Measures[lvlTrIndex]["max."+valueCSVAttr]);
        return (value - min) / (max - min);
    }

    private float CondIncMBS()
    {
        float condInc = 0.0f;
        if (game.CurrLvl > 0) //transition 00 does not make sense, so do nothing to condition
        {
            int lvlTrIndex = (game.PrevLvl * game.NumLvls + game.CurrLvl) - 1; //transition 00 does not make sense
            
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
    private float CondIncTheKite()
    {
        float condInc = 0.0f;
        if (game.CurrLvl > 0) //transition 00 does not make sense, so do nothing to condition
        {
            int lvlTrIndex = (game.PrevLvl * game.NumLvls + game.CurrLvl) - 1; //transition 00 does not make sense
            condInc += 1.0f - NormalizeFromCSV(lvlTrIndex,"stressLevel_delta.second_played_lvl.thekite");
            condInc += 1.0f - NormalizeFromCSV(lvlTrIndex,"heartRate_delta.second_played_lvl.thekite");
            condInc += NormalizeFromCSV(lvlTrIndex,"RSQ_thekite_score_delta");
            condInc /= 3.0f;
        }
        return condInc;
    }
        
    public override void Initialize()
    {
        // for Mr. Blue Sky
        config = new SimConfig(3,
             new List<string>() {"A", "B", "C"},
            "ExpData/processed_data_mrbluesky_bytransition",
            CondIncMBS);
        
        // for The Kite
        // config = new SimConfig(4,
        //     new List<string>() {"A", "B", "C", "D"},
        //     "ExpData/processed_data_thekite_bytransition",
        //     CondIncTheKite);
        
        game = new GameWrapper(gameUI, config);
        patient = new PatientWrapper(config);
        
        numCellsPerDim = game.NumLvls + 1;
        if (initUI)
        {
            freqHeatmapCells = new List<Image>();
            float freqHeatmapWidth = freqHeatmap.GetComponent<RectTransform>().sizeDelta.x;
            float cellWidth = freqHeatmapWidth / numCellsPerDim;
            freqHeatmap.GetComponent<GridLayoutGroup>().cellSize= new Vector2(cellWidth*0.9f,cellWidth*0.9f);

            
            List<string> axisLabels = config.LvlNames;
            axisLabels.Insert(0, "_");
            for (int i = 0; i < (numCellsPerDim * numCellsPerDim); i++)
            {
                if (i < numCellsPerDim) //works because it is symmetrical
                {
                    Instantiate(freqHeatmapAxisMarkPrefab, freqHeatmapAxisMarksX.transform).GetComponent<TMP_Text>()
                        .text = axisLabels[i];
                    Instantiate(freqHeatmapAxisMarkPrefab, freqHeatmapAxisMarksY.transform).GetComponent<TMP_Text>()
                        .text = axisLabels[i];
                }

                Image cell = Instantiate(freqHeatmapSquarePrefab, freqHeatmap.transform)
                    .GetComponent<Image>();
                freqHeatmapCells.Add(cell);
                if(i==0) //the material affects all cells
                    cell.material.color = new Color(1.0f, 1.0f, 1.0f);
                cell.color = new Color(1.0f, 1.0f, 1.0f);
            }
            // freqHeatmapCells[0*4+0].color = new Color(0.0f,1.0f,1.0f);
            // freqHeatmapCells[0*4+3].color = new Color(0.0f,0.0f,1.0f);
            // freqHeatmapCells[3*4+0].color = new Color(1.0f,0.0f,0.0f);
            // freqHeatmapCells[3*4+3].color = new Color(1.0f,1.0f,0.0f);
        }
        // else
        // {
        //     freqHeatmapCells = new List<Image>(freqHeatmap.GetComponentsInChildren<Image>());
        // }

        currDDAStrat = new List<int>();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(game.PrevLvl);
        sensor.AddObservation(game.CurrLvl);
    }

    public void PrintBestStratSoFar()
    {
        string stratStr = "[";
        foreach (var action in currDDAStrat)
        {
            stratStr += action + " -> ";
        }
        stratStr += "]";
        Debug.Log("Best episode so far:"+stratStr);
        Debug.Log("pInc:"+pInc);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        if (patient.PlayedLvls > 0) //force start at state 0
            game.CurrLvl = actionBuffers.DiscreteActions[0];
        else
            game.CurrLvl = 0;

        patient.PlayGame(game);
        currDDAStrat.Add(game.CurrLvl);
        if (patient.PlayedLvls > 10)
        {
            //update plot
            for (int i=1; i<currDDAStrat.Count; i++)
            {
                Image mesh = freqHeatmapCells[numCellsPerDim * currDDAStrat[i] + currDDAStrat[i-1]];
                mesh.color -= new Color(0.0f, 0.003f, 0.003f,0.0f);
            }
            foreach (var mesh2 in freqHeatmapCells)
            {
                mesh2.color += new Color(0.0f, 0.001f, 0.001f);
            }
            
            // float newPInc = (patient.Condition - patient.PrevCondition)/ patient.PlayedLvls;
            float newPInc = patient.Condition/ patient.PlayedLvls;
            SetReward(newPInc);
            EndEpisode();
        }
        
    }

    public override void OnEpisodeBegin()
    {
        currDDAStrat.Clear();
        patient.InitRun();
        // patient.PrevCondition = patient.Condition;
        patient.PlayedLvls = 0;
        game = new GameWrapper(gameUI, config);

        if (!initUI)
        {
            freqHeatmapCells = new List<Image>(freqHeatmap.GetComponentsInChildren<Image>());
            // freqHeatmapCells[0*4+0].color = new Color(0.0f,1.0f,1.0f);
            // freqHeatmapCells[0*4+3].color = new Color(0.0f,0.0f,1.0f);
            // freqHeatmapCells[3*4+0].color = new Color(1.0f,0.0f,0.0f);
            // freqHeatmapCells[3*4+3].color = new Color(1.0f,1.0f,0.0f);
            initUI = true;
        }
    }

    // public override void Heuristic(in ActionBuffers actionsOut)
    // {
    //     var discreteActions = actionsOut.DiscreteActions;
    //     discreteActions[0] = Random.Range(0,5);
    // }
}
