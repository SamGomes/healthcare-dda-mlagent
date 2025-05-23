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
        
        private int m_CsvI; //for changing when in dynamic behaviour mode
        private string[] m_TransitionCSVPath; //for changing when in dynamic behaviour mode
        
        public Func<int,List<(int,int)>,int,int,int,float> RewardFunc { get; set; }

        public int MeanFlareDuration;
        
        
        
        public SimConfig(
            string algName,
            string gameCond,
            AgentManager.FlareModCond flareModCond,
            int numEpisodeLvls, 
            int numGameLvls, 
            List<string> nameGameLvls, 
            string[] transitionCSVPath, 
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

            m_TransitionCSVPath = transitionCSVPath;
            
            MeanFlareDuration = meanFlareDuration;
        }

        public void ChangeBehavior(int behaviorI)
        {
            Debug.Log("Changing behavior, if dynamic.");
            string curr_m_TransitionCSVPath = m_TransitionCSVPath[behaviorI % m_TransitionCSVPath.Length];
            Measures = CSVReader.Read(curr_m_TransitionCSVPath);
            Debug.Log("Loaded: \""+curr_m_TransitionCSVPath+"\"");
        }
        
    }
    public class PatientWrapper
    {
        private SimConfig m_Config;
        
        public int PlayedLvls { get; set; }
        public float Condition { get; private set; }

        public float PrevCondition { get; private set; }
        
        public List<(int,int)> Flares;

        private int m_behaviorI;
        
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
            m_behaviorI = -1;
            ChangeBehavior();
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
            float conditionInc = m_Config.RewardFunc(PlayedLvls,Flares,game.PrevLvl,game.CurrLvl,game.NumLvls);
            Condition += conditionInc;
            PlayedLvls++;
        }

        public void ChangeBehavior()
        {
            m_behaviorI++;
            m_Config.ChangeBehavior(m_behaviorI);
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
