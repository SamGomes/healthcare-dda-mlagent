using System;
using System.Collections.Generic;
using Google.Protobuf.WellKnownTypes;
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

        private float NormalizeFromCSV(int lvlTrIndex, string valueCSVAttr)
        {
            float value = Convert.ToSingle(m_Measures[lvlTrIndex][valueCSVAttr]); // cast to float did not work for some reason
            float min = Convert.ToSingle(m_Measures[lvlTrIndex]["min."+valueCSVAttr]);
            float max = Convert.ToSingle(m_Measures[lvlTrIndex]["max."+valueCSVAttr]);
            return (value - min) / (max - min);
        }

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
            if (game.CurrLvl > 0) //transition 00 does not make sense, so do nothing to condition
            {
                int lvlTrIndex = (game.PrevLvl * game.NumLvls + game.CurrLvl) - 1; //transition 00 does not make sense
                float condInc = 0.0f;
                condInc += NormalizeFromCSV(lvlTrIndex,"averageTimeRight.second_played_lvl");
                condInc += NormalizeFromCSV(lvlTrIndex,"averageTimeLeft.second_played_lvl");
                condInc += 1.0f - NormalizeFromCSV(lvlTrIndex,"stressLevel_delta.second_played_lvl.mrbluesky");
                condInc += 1.0f - NormalizeFromCSV(lvlTrIndex,"heartRate_delta.second_played_lvl.mrbluesky");
                condInc += NormalizeFromCSV(lvlTrIndex,"average_displacement1_mrbluesky.second_played_lvl");
                condInc += NormalizeFromCSV(lvlTrIndex,"average_displacement2_mrbluesky.second_played_lvl");
                condInc += NormalizeFromCSV(lvlTrIndex,"RSQ_mrbluesky_score_delta");
                Condition += condInc/7.0f; 
            }
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
