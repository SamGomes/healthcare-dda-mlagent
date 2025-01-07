using System.Collections.Generic;
using SimEntities;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class DDAAgent : Agent
{
    private GameWrapper game;
    private PatientWrapper patient;

    public GameObject gameUI;
    
    public GameObject freqHeatmap;
    private MeshRenderer[] freqHeatmapMeshes;
    
    private List<int> currDDAStrat;
    private float pInc;

    public override void Initialize()
    {
        game = new GameWrapper(gameUI);
        patient = new PatientWrapper();

        freqHeatmapMeshes = freqHeatmap.GetComponentsInChildren<MeshRenderer>();
        foreach(var mesh in freqHeatmapMeshes)
        {
            mesh.material.color = new Color(1.0f,1.0f,1.0f);
        }
        // freqHeatmapMeshes[3*7+3].material.color = new Color(1.0f,0.0f,1.0f);
        // freqHeatmapMeshes[0*7+0].material.color = new Color(0.0f,1.0f,1.0f);
        // freqHeatmapMeshes[0*7+6].material.color = new Color(0.0f,0.0f,1.0f);
        // freqHeatmapMeshes[6*7+0].material.color = new Color(1.0f,0.0f,0.0f);
        // freqHeatmapMeshes[6*7+6].material.color = new Color(1.0f,1.0f,0.0f);
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
        game.CurrLvl = actionBuffers.DiscreteActions[0] + 1;
        patient.PlayGame(game);
        currDDAStrat.Add(game.CurrLvl);
        if (patient.PlayedLvls > (game.NumLvls - 1))
        {
            float newPInc = (patient.Condition - patient.PrevCondition)/ 7.0f;
            if (newPInc > pInc)
            {
                pInc = newPInc;
                PrintBestStratSoFar();
            }
            SetReward(newPInc);
            EndEpisode();
        }
        if (patient.PlayedLvls > 1)
        {
            MeshRenderer mesh = freqHeatmapMeshes[game.NumLvls * (game.PrevLvl - 1) + (game.CurrLvl - 1)];
            mesh.material.color -= new Color(0.0f, 0.0011f, 0.0011f);
            
            foreach(var mesh2 in freqHeatmapMeshes)
            {
                mesh2.material.color += new Color(0.0f, 0.000001f, 0.000001f);
            }
        }
    }

    public override void OnEpisodeBegin()
    {
        currDDAStrat.Clear();
        patient.PrevCondition = patient.Condition;
        patient.PlayedLvls = 0;
        game = new GameWrapper(gameUI);
    }

    // public override void Heuristic(in ActionBuffers actionsOut)
    // {
    //     var discreteActions = actionsOut.DiscreteActions;
    //     discreteActions[0] = Random.Range(0,5);
    // }
}
