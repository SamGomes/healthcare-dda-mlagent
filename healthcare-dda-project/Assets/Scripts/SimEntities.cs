using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace SimEntities
{

    public class PatientWrapper
    {
        public int PlayedLvls { get; set; } = 0;
        public float PrevCondition { get; set; } = 0;
        public float Condition { get; private set; } = 0.0f;

        //sets the improveRate to a random value between 0.4 and 0.7
        private float ImprovRate { get; } = Random.Range(0.5f,1.0f);

        public int[] PrefLvlSeq { get; } = {1,2,3,4,4,4,7};

        public void PlayGame(GameWrapper game)
        {
            float currLvlPref = ((float) game.NumLvls - math.abs(PrefLvlSeq[PlayedLvls] - game.CurrLvl))/ game.NumLvls;
            float conditionDelta = currLvlPref;
            Condition += conditionDelta;
            PlayedLvls++;
        }
    }

    public class GameWrapper
    {
        public int NumLvls { get; set; } = 7;
        public Slider gameUI;

        //represents a game level transition
        public int PrevLvl { get; set; } = 1;
        private int m_CurrLvl = 1;
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
