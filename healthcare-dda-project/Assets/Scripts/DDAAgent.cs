using System.Collections.Generic;
using System.IO;
using SimEntities;
using TMPro;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine.UI;


public class DDAAgent : Agent
{
    private GameWrapper m_Game;
    private PatientWrapper m_Patient;

    public bool initUI;
    public SimConfig Config;
    
    public GameObject gameUI;
    
    public GameObject freqHeatmap;
    public GameObject freqHeatmapSquarePrefab;
    private List<Image> m_FreqHeatmapCells;
    public GameObject freqHeatmapAxisMarksX;
    public GameObject freqHeatmapAxisMarksY;
    public GameObject freqHeatmapAxisMarkPrefab;
    
    private List<int> m_CurrDDAStrat;
    private List<string> m_CurrDDATjs;
    private float m_PInc;
    
    private int m_NumCellsPerDim;

    private int m_InitialLvl;
    
    public override void Initialize()
    {
        freqHeatmap = GameObject.Find("TrialGrid/gridSquares");
        freqHeatmapAxisMarksX = GameObject.Find("TrialGrid/xAxis/marks");
        freqHeatmapAxisMarksY = GameObject.Find("TrialGrid/yAxis/marks");
        Config = GameObject.Find("AgentManager").GetComponent<AgentManager>().Config;
        initUI = GameObject.Find("Agents").transform.childCount == 1;

        m_Game = new GameWrapper(gameUI, Config);
        m_Patient = new PatientWrapper(Config);

        m_InitialLvl = 0;

        m_NumCellsPerDim = m_Game.NumLvls + 1;
        
        if (initUI)
        {
            m_FreqHeatmapCells = new List<Image>();
            float freqHeatmapWidth = freqHeatmap.GetComponent<RectTransform>().sizeDelta.x;
            float cellWidth = freqHeatmapWidth / m_NumCellsPerDim;
            freqHeatmap.GetComponent<GridLayoutGroup>().cellSize= new Vector2(cellWidth*0.9f,cellWidth*0.9f);

            
            List<string> axisLabels = Config.NameGameLvls;
            axisLabels.Insert(0, "_");
            for (int i = 0; i < (m_NumCellsPerDim * m_NumCellsPerDim); i++)
            {
                if (i < m_NumCellsPerDim) //works because it is symmetrical
                {
                    Instantiate(freqHeatmapAxisMarkPrefab, freqHeatmapAxisMarksX.transform).GetComponent<TMP_Text>()
                        .text = axisLabels[i];
                    Instantiate(freqHeatmapAxisMarkPrefab, freqHeatmapAxisMarksY.transform).GetComponent<TMP_Text>()
                        .text = axisLabels[i];
                }

                Image cell = Instantiate(freqHeatmapSquarePrefab, freqHeatmap.transform)
                    .GetComponent<Image>();
                m_FreqHeatmapCells.Add(cell);
                if(i==0) //the material affects all cells
                    cell.material.color = new Color(1.0f, 1.0f, 1.0f,1.0f);
                cell.color = new Color(1.0f, 1.0f, 1.0f,1.0f);
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

        m_CurrDDAStrat = new List<int>();
        m_CurrDDATjs = new List<string>();
        for (int i = 0; i < m_Game.NumLvls + 1; i++)
        {
            m_CurrDDATjs.Add("");
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(m_Game.PrevLvl);
        sensor.AddObservation(m_Game.CurrLvl);
    }

    private void UpdateFreqHeatmapCells()
    {
        m_CurrDDATjs[m_InitialLvl] = "["+ m_CurrDDAStrat[0]+"->";
        //update heatmap
        for (int i=1; i<m_CurrDDAStrat.Count; i++)
        {
            Image mesh = m_FreqHeatmapCells[m_NumCellsPerDim * m_CurrDDAStrat[i] + m_CurrDDAStrat[i-1]];
            mesh.color -= mesh.color.g < 0.0f? Color.clear : new Color(0.0f, 0.003f, 0.003f,0.0f);

            m_CurrDDATjs[m_InitialLvl] += (i<m_CurrDDAStrat.Count - 1)? m_CurrDDAStrat[i]+"->": m_CurrDDAStrat[i];

            for (int j = 0; j < m_Game.NumLvls + 1; j++)
            {
                Image mesh2 = m_FreqHeatmapCells[m_NumCellsPerDim * j + m_CurrDDAStrat[i-1]];
                mesh2.color += mesh2.color.g > 1.0f? Color.clear : new Color(0.0f, 0.001f, 0.001f,0.0f);
            }
        }
        m_CurrDDATjs[m_InitialLvl] += ']';
    }
    
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        if (m_Patient.PlayedLvls > 0) //force start at state 0
            m_Game.CurrLvl = actionBuffers.DiscreteActions[0];
        else
            m_Game.CurrLvl = m_InitialLvl;

        m_Patient.PlayGame(m_Game);
        m_CurrDDAStrat.Add(m_Game.CurrLvl);
        
        // set incentive reward (dense)
        float newPInc = (m_Patient.Condition - m_Patient.PrevCondition);
        SetReward(newPInc);
        
        if (m_Patient.PlayedLvls >= Config.NumEpisodeLvls)
        {
            UpdateFreqHeatmapCells();
            
            // if using reward at end of episode (sparse)
            // float newPInc = m_Patient.Condition/ m_Patient.PlayedLvls;
            // SetReward(newPInc);
        
            m_InitialLvl = (m_InitialLvl > m_Game.NumLvls - 1) ? 0 : m_InitialLvl + 1;
            EndEpisode();
        }
        
    }

    private void SaveCurrentConvState()
    {
        string folderPath = "Assets/VisualResults/Screenshots/"+Config.AlgName+"/"; // the path of your project folder

        if (!Directory.Exists(folderPath)) // if this path does not exist yet
            Directory.CreateDirectory(folderPath);  // it will get created
    
        var screenshotName =
            "Screenshot_" +
            Config.GameCond + // puts the current env setting into the screenshot name
            ".png";
        ScreenCapture.CaptureScreenshot(Path.Combine(folderPath, screenshotName),2); // takes the sceenshot, the "2" is for the scaled resolution, you can put this to 600 but it will take really long to scale the image up
        
        folderPath = "Assets/VisualResults/ConvTrajectories/"+Config.AlgName+"/"; // the path of your project folder

        if (!Directory.Exists(folderPath)) // if this path does not exist yet
            Directory.CreateDirectory(folderPath); 
        var stratName =
            "Trajectories_" +
            Config.GameCond +
            ".txt";
        StreamWriter writer = new StreamWriter(Path.Combine(folderPath, stratName), false);
        
        for (int i = 0; i < m_Game.NumLvls; i++)
        {
            writer.WriteLine(m_CurrDDATjs[i]);
        }
        writer.Close();
        
        Debug.Log(folderPath + screenshotName); // You get instant feedback in the console
    }
    
    public override void OnEpisodeBegin()
    {
        if(CompletedEpisodes == 3100) 
            SaveCurrentConvState();
        
        m_CurrDDAStrat.Clear();
        m_Patient.InitRun();
        // patient.PrevCondition = patient.Condition;
        m_Patient.PlayedLvls = 0;
        m_Game = new GameWrapper(gameUI, Config);
        
        if (!initUI)
        {
            m_FreqHeatmapCells = new List<Image>(freqHeatmap.GetComponentsInChildren<Image>());
            // freqHeatmapCells[0*4+0].color = new Color(0.0f,1.0f,1.0f);
            // freqHeatmapCells[0*4+3].color = new Color(0.0f,0.0f,1.0f);
            // freqHeatmapCells[3*4+0].color = new Color(1.0f,0.0f,0.0f);
            // freqHeatmapCells[3*4+3].color = new Color(1.0f,1.0f,0.0f);
            initUI = true;
        }
    }

    // public override void Heuristic(in ActionBuffers actionsOut)
    // {
    //     // game.CurrLvl = actionsOut.DiscreteActions[0];
    //     // patient.PlayGame(game);
    //     // currDDAStrat.Add(game.CurrLvl);
    //
    //     currDDAStrat = new List<int> { 0, 2, 1, 1, 1 };
    //     foreach (var mesh2 in freqHeatmapCells)
    //     {
    //         mesh2.color = new Color(1.0f, 1.0f, 1.0f,1.0f);
    //     }
    //     // for (int i=1; i<currDDAStrat.Count; i++)
    //     // {
    //     //     Image mesh = freqHeatmapCells[numCellsPerDim * currDDAStrat[i] + currDDAStrat[i-1]];
    //     //     mesh.color = new Color(1.0f, 0.0f, 0.0f,1.0f);
    //     // }
    // }
    
}
