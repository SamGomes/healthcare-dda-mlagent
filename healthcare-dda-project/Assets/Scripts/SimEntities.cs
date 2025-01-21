using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace SimEntities
{

    public class PatientWrapper
    {
        
        private DDAAgent m_DdaAgentObject;
        
        private LogManager m_LogManager = new FileLogManager();
        private string m_PatientPlayerId;
        private string m_PatientRunId;
        
        public int PlayedLvls { get; set; } = 0;
        public float PrevCondition { get; set; } = 0;
        public float Condition { get; private set; } = 0.0f;

        //sets the improveRate to a random value between 0.4 and 0.7
        private float ImprovRate { get; } = Random.Range(0.5f,1.0f);

        public int[] PrefLvlSeq { get; } = {1,2,3,4,5,6,7};
        // public int[] PrefLvlSeq { get; } = {7,2,3,1,4,5,6};


        public PatientWrapper(DDAAgent ddaAgentObject)
        {
            m_DdaAgentObject = ddaAgentObject;
            m_PatientPlayerId = Random.Range(1, 1000) +
                                Random.Range(1, 1000) +
                                Random.Range(1, 1000).ToString();
        }
        public void InitRun()
        {
            m_PatientRunId = Random.Range(1, 1000) +
                                Random.Range(1, 1000) +
                                Random.Range(1, 1000).ToString();
        }
        public void PlayGame(GameWrapper game)
        {
            float currLvlPref = ((float) game.NumLvls - math.abs(PrefLvlSeq[PlayedLvls] - game.CurrLvl))/ game.NumLvls;
            float conditionDelta = currLvlPref;
            float prevCondition = Condition;
            Condition += conditionDelta;
            PlayedLvls++;

            Dictionary<string, string> dict = new Dictionary<string, string>()
            {
                { "patient_id", m_PatientPlayerId },
                { "run_id", m_PatientRunId },
                { "num_played_levels", PlayedLvls.ToString() },
                { "level", game.CurrLvl.ToString() },
                { "initial_condition", prevCondition.ToString() },
                { "score", Random.Range(500, 10000).ToString() },
                { "play_time", Random.Range(5000, 100000).ToString() },
                { "final_condition", Condition.ToString() }
            };
            
            
            m_DdaAgentObject.StartCoroutine(
                m_LogManager.WriteToLog("db", "table",
                    dict, false)
                );
            
        }
    }

    public class GameWrapper
    {
        public int NumLvls { get; set; } = 7;
        public Slider gameUI;

        //represents a game level transition
        public int PrevLvl { get; set; } = 0;
        private int m_CurrLvl = 0;
        public int CurrLvl
        {
            get
            {
                return m_CurrLvl;
            }
            set
            {
                PrevLvl = m_CurrLvl;
                m_CurrLvl = value;
                gameUI.value = value;
            }
        }

        public GameWrapper(GameObject gameUI)
        {
            this.gameUI = gameUI.GetComponent<Slider>();
        }

    }
}
