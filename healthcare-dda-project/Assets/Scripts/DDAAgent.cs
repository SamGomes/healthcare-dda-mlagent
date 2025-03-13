using System.Collections.Generic;
using SimEntities;
using TMPro;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine.UI;



public class DDAAgent : Agent
{
    private GameWrapper game;
    private PatientWrapper patient;

    public bool initUI;
    public SimConfig Config;
    
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

    
    public override void Initialize()
    {
        freqHeatmap = GameObject.Find("TrialGrid/gridSquares");
        freqHeatmapAxisMarksX = GameObject.Find("TrialGrid/xAxis/marks");
        freqHeatmapAxisMarksY = GameObject.Find("TrialGrid/yAxis/marks");
        Config = GameObject.Find("AgentManager").GetComponent<AgentManager>().Config;
        initUI = GameObject.Find("Agents").transform.childCount == 1;

        game = new GameWrapper(gameUI, Config);
        patient = new PatientWrapper(Config);

        numCellsPerDim = game.NumLvls + 1;
        if (initUI)
        {
            freqHeatmapCells = new List<Image>();
            float freqHeatmapWidth = freqHeatmap.GetComponent<RectTransform>().sizeDelta.x;
            float cellWidth = freqHeatmapWidth / numCellsPerDim;
            freqHeatmap.GetComponent<GridLayoutGroup>().cellSize= new Vector2(cellWidth*0.9f,cellWidth*0.9f);

            
            List<string> axisLabels = Config.LvlNames;
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
        // Debug.Log("Best episode so far:"+stratStr);
        // Debug.Log("pInc:"+pInc);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        if (patient.PlayedLvls > 0) //force start at state 0
            game.CurrLvl = actionBuffers.DiscreteActions[0];
        else
            game.CurrLvl = 0;

        patient.PlayGame(game);
        currDDAStrat.Add(game.CurrLvl);
        if (patient.PlayedLvls >= 10)
        {
            //update heatmap
            for (int i=1; i<currDDAStrat.Count; i++)
            {
                Image mesh = freqHeatmapCells[numCellsPerDim * currDDAStrat[i] + currDDAStrat[i-1]];
                mesh.color -= new Color(0.0f, 0.003f, 0.003f,0.0f);
            }
            foreach (var mesh2 in freqHeatmapCells)
            {
                mesh2.color += new Color(0.0f, 0.001f, 0.001f,0.0f);
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
        game = new GameWrapper(gameUI, Config);

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
