using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SimEntities
{

    public class SimConfig
    {
        public int NumLvls { get; set; }
        public List<string> LvlNames { get; }
        
        public List<Dictionary<string,object>> Measures;
        public Func<int,int,int,float> RewardFunc { get; set; }

        public SimConfig(int numLvls, 
            List<string> lvlNames, 
            string transitionCSVPath, 
            Func<int,int,int,float> rewardFunc)
        {
            NumLvls = numLvls;
            LvlNames = lvlNames;
            RewardFunc = rewardFunc;
            Measures = CSVReader.Read (transitionCSVPath);
        }
    }
    public class PatientWrapper
    {
        private SimConfig m_Config;
        
        public int PlayedLvls { get; set; }
        public float Condition { get; private set; }

        
        public PatientWrapper(SimConfig config)
        {
            m_Config = config;
        }
        public void InitRun()
        {
            Condition = 0.0f;
        }
        public void PlayGame(GameWrapper game)
        {
            Condition += m_Config.RewardFunc(game.PrevLvl,game.CurrLvl,game.NumLvls);
            PlayedLvls++;
        }
    }

    public class GameWrapper
    {
        public int NumLvls { get; }
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

        public GameWrapper(GameObject gameUI, SimConfig config)
        {
            NumLvls = config.NumLvls;
            this.gameUI = gameUI.GetComponent<Slider>();
        }

    }
}
