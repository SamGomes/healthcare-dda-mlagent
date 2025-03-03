using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace SimEntities
{

    public class PatientWrapper
    {
        
        private DDAAgent m_DdaAgentObject;
        
        // private LogManager m_LogManager = new FileLogManager();
        private List<Dictionary<string,object>> m_Measures;
        
        public int PlayedLvls { get; set; } = 0;
        public float PrevCondition { get; set; } = 0;
        public float Condition { get; private set; } = 0.0f;

        //sets the improveRate to a random value between 0.4 and 0.7
        private float ImprovRate { get; } = Random.Range(0.5f,1.0f);



        public PatientWrapper(DDAAgent ddaAgentObject)
        {
            m_DdaAgentObject = ddaAgentObject;
            m_Measures = CSVReader.Read ("ExpData/processed_data_mrbluesky_bytransition");
        }
        public void InitRun()
        {
        }
        public void PlayGame(GameWrapper game)
        {
            
            int lvlTrIndex = (game.PrevLvl * game.NumLvls + game.CurrLvl) - 1; //transition 00 does not make sense
            Debug.Log(lvlTrIndex);
            float normalizedRSQScore = Convert.ToSingle(m_Measures[lvlTrIndex]["RSQ_mrbluesky_score"]) / 50.0f;
            Condition += normalizedRSQScore; // cast to float did not work for some reason
            PlayedLvls++;
            
        }
    }

    public class GameWrapper
    {
        public int NumLvls { get; set; } = 3;
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
