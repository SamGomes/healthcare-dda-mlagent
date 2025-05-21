using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SimEntities
{

    public class SimConfig
    {
        public string AlgName { get; set; }
        
        public string GameCond { get; set; }
        public AgentManager.FlareModCond FlareModCond { get; set; }
        
        public int NumEpisodeLvls { get; set; }
        public int NumGameLvls { get; set; }
        public List<string> NameGameLvls { get; }
        
        public List<Dictionary<string,object>> Measures;
        public Func<int,List<(int,int)>,int,int,int,float> RewardFunc { get; set; }

        public int MeanFlareDuration;
        
        
        
        public SimConfig(
            string algName,
            string gameCond,
            AgentManager.FlareModCond flareModCond,
            int numEpisodeLvls, 
            int numGameLvls, 
            List<string> nameGameLvls, 
            string transitionCSVPath, 
            Func<int,List<(int,int)>,int,int,int,float> rewardFunc,
            int meanFlareDuration)
        {
            AlgName = algName;
            GameCond = gameCond;
            FlareModCond = flareModCond;
            NumEpisodeLvls = numEpisodeLvls;
            NumGameLvls = numGameLvls;
            NameGameLvls = nameGameLvls;
            RewardFunc = rewardFunc;
            Measures = CSVReader.Read(transitionCSVPath);
            
            MeanFlareDuration = meanFlareDuration;
        }
    }
    public class PatientWrapper
    {
        private SimConfig m_Config;
        
        public int PlayedLvls { get; set; }
        public float Condition { get; private set; }

        public float PrevCondition { get; private set; }
        
        public List<(int,int)> Flares;
        
        
        public static float RandomGaussian(float minValue = 0.0f, float maxValue = 1.0f)
        {
            float u, v, S;

            do
            {
                u = 2.0f * Random.value - 1.0f;
                v = 2.0f * Random.value - 1.0f;
                S = u * u + v * v;
            }
            while (S >= 1.0f);

            // Standard Normal Distribution
            float std = u * Mathf.Sqrt(-2.0f * Mathf.Log(S) / S);

            // Normal Distribution centered between the min and max value
            // and clamped following the "three-sigma rule"
            float mean = (minValue + maxValue) / 2.0f;
            float sigma = (maxValue - mean) / 3.0f;
            return Mathf.Clamp(std * sigma + mean, minValue, maxValue);
        }
        
        public PatientWrapper(SimConfig config)
        {
            m_Config = config;
        }
        public void InitRun()
        {
            Flares = new List<(int, int)>();
            int numFlares = Random.Range(0, 3);

            // List<int> xtest = new List<int>(200);
            // for (int i = 0; i < 200; i++)
            //     xtest.Add(0);
            // for (int i = 0; i < 10000; i++)
            // {
            //     int flareVar = (int) RandomGaussian(0,2*m_Config.MeanFlareDuration);
            //     xtest[flareVar]++;
            // }
            // Debug.Log(xtest);
            
            for (int i = 0; i < numFlares; i++)
            {
                float flareVar = RandomGaussian(0,2*m_Config.MeanFlareDuration);
                int flareStd = (int) (flareVar / 2.0f);
                int flareMax = Random.Range(-flareStd + 1, m_Config.NumEpisodeLvls + flareStd);
                Flares.Add((flareMax,2*flareStd));
            }
            
            Condition = 0.0f;
            PrevCondition = 0.0f;
        }
        public void PlayGame(GameWrapper game)
        {
            PrevCondition = Condition;
            Condition += m_Config.RewardFunc(PlayedLvls,Flares,game.PrevLvl,game.CurrLvl,game.NumLvls);
            PlayedLvls++;
        }
    }

    public class GameWrapper
    {
        public int NumLvls { get; }

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
            }
        }

        public GameWrapper(GameObject gameUI, SimConfig config)
        {
            NumLvls = config.NumGameLvls;
        }

    }
}
