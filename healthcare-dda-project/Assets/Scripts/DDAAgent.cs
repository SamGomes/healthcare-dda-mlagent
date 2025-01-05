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

    private List<int> currPolicy;
    private float pInc;

    public override void Initialize()
    {
        game = new GameWrapper(gameUI);
        patient = new PatientWrapper();

        currPolicy = new List<int>();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(game.PrevLvl);
        sensor.AddObservation(game.CurrLvl);
    }

    public void PrintBestPolicySoFar()
    {
        string policyStr = "[";
        foreach (var action in currPolicy)
        {
            policyStr += action + " -> ";
        }
        policyStr += "]";
        Debug.Log("Last policy found:"+policyStr);
        Debug.Log("pInc:"+pInc);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        game.CurrLvl = actionBuffers.DiscreteActions[0] + 1;
        patient.PlayGame(game);
        currPolicy.Add(game.CurrLvl);
        if (patient.PlayedLvls > (game.NumLvls - 1))
        {
            float newPInc = (patient.Condition - patient.PrevCondition);
            if (newPInc > pInc)
            {
                pInc = newPInc;
                PrintBestPolicySoFar();
            }
            SetReward(newPInc);
            EndEpisode();
        }
    }

    public override void OnEpisodeBegin()
    {
        currPolicy.Clear();
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
