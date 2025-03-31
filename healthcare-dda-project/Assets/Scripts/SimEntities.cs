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

        public int FlareDuration;
        
        public SimConfig(int numEpisodeLvls, 
            int numGameLvls, 
            List<string> nameGameLvls, 
            string transitionCSVPath, 
            Func<int,List<(int,int)>,int,int,int,float> rewardFunc,
            int flareDuration)
        {
            NumEpisodeLvls = numEpisodeLvls;
            NumGameLvls = numGameLvls;
            NameGameLvls = nameGameLvls;
            RewardFunc = rewardFunc;
            Measures = CSVReader.Read (transitionCSVPath);
            
            FlareDuration = flareDuration;
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
        }
        public void InitRun()
        {
            Flares = new List<(int, int)>();

            int numFlares = Random.Range(0, 3);
            for (int i = 0; i < numFlares; i++)
            {
                int flareVar = m_Config.FlareDuration;
                int flareMax = Random.Range(-flareVar + 1, m_Config.NumEpisodeLvls + flareVar);
                Flares.Add((flareMax,flareVar));
            }
            
            Condition = 0.0f;
        }
        public void PlayGame(GameWrapper game)
        {
            Condition += m_Config.RewardFunc(PlayedLvls,Flares,game.PrevLvl,game.CurrLvl,game.NumLvls);
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
            NumLvls = config.NumGameLvls;
            this.gameUI = gameUI.GetComponent<Slider>();
        }

    }
}
