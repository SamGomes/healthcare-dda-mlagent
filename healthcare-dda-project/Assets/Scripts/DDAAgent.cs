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
    
    public override void Initialize()
    {
        
        game = new GameWrapper(gameUI);
        patient = new PatientWrapper(this);
        
        if (initUI)
        {
            freqHeatmapCells = new List<Image>();
            float freqHeatmapWidth = freqHeatmap.GetComponent<RectTransform>().sizeDelta.x;
            float cellWidth = freqHeatmapWidth / game.NumLvls;
            freqHeatmap.GetComponent<GridLayoutGroup>().cellSize= new Vector2(cellWidth*0.9f,cellWidth*0.9f);
            for (int i = 0; i < (game.NumLvls * game.NumLvls); i++)
            {
                if (i < game.NumLvls) //works because it is symmetrical
                {
                    Instantiate(freqHeatmapAxisMarkPrefab, freqHeatmapAxisMarksX.transform).GetComponent<TMP_Text>()
                        .text = i.ToString();
                    Instantiate(freqHeatmapAxisMarkPrefab, freqHeatmapAxisMarksY.transform).GetComponent<TMP_Text>()
                        .text = i.ToString();
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
        game.CurrLvl = actionBuffers.DiscreteActions[0];
        patient.PlayGame(game);
        currDDAStrat.Add(game.CurrLvl);
        if (patient.PlayedLvls > (game.NumLvls - 1))
        {
            // float newPInc = (patient.Condition - patient.PrevCondition)/ 7.0f;
            // if (newPInc > pInc)
            // {
            //     pInc = newPInc;
            //     // PrintBestStratSoFar();
            // }
            SetReward(patient.Condition);
            EndEpisode();
        }
        if (patient.PlayedLvls > 1)
        {
            Image mesh = freqHeatmapCells[game.NumLvls * game.CurrLvl + game.PrevLvl];
            mesh.color -= new Color(0.0f, 0.01f, 0.01f);
            foreach(var mesh2 in freqHeatmapCells)
            {
                mesh2.color += new Color(0.0f, 0.0001f, 0.0001f);
            }
        }
    }

    public override void OnEpisodeBegin()
    {
        currDDAStrat.Clear();
        patient.InitRun();
        patient.PrevCondition = patient.Condition;
        patient.PlayedLvls = 0;
        game = new GameWrapper(gameUI);

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
