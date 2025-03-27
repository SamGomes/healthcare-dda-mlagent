using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace SimEntities
{

    public class SimConfig
    {
        public int NumEpisodeLvls { get; set; }
        public int NumGameLvls { get; set; }
        public List<string> NameGameLvls { get; }
        
        public List<Dictionary<string,object>> Measures;
        public Func<int,List<(int,int)>,int,int,int,float> RewardFunc { get; set; }

        public SimConfig(int numEpisodeLvls, 
            int numGameLvls, 
            List<string> nameGameLvls, 
            string transitionCSVPath, 
            Func<int,List<(int,int)>,int,int,int,float> rewardFunc)
        {
            NumEpisodeLvls = numEpisodeLvls;
            NumGameLvls = numGameLvls;
            NameGameLvls = nameGameLvls;
            RewardFunc = rewardFunc;
            Measures = CSVReader.Read (transitionCSVPath);
        }
    }
    public class PatientWrapper
    {
        private SimConfig m_Config;
        
        public int PlayedLvls { get; set; }
        public float Condition { get; private set; }

        public List<(int,int)> Flares;
        
        public PatientWrapper(SimConfig config)
        {
            m_Config = config;
            Flares = new List<(int, int)>();

            int numFlares = Random.Range(0, 3);
            for (int i = 0; i < numFlares; i++)
            {
                int flareMax = Random.Range(0, m_Config.NumEpisodeLvls + 1);
                int flareRange = 15;
                Flares.Add((flareMax,15));
            }
        }
        public void InitRun()
        {
            Condition = 0.0f;
        }
        public void PlayGame(GameWrapper game)
        {
            PlayedLvls++;
            Condition += m_Config.RewardFunc(PlayedLvls,Flares,game.PrevLvl,game.CurrLvl,game.NumLvls);
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
            NumLvls = config.NumGameLvls;
            this.gameUI = gameUI.GetComponent<Slider>();
        }

    }
}
