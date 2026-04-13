using Il2CppAssets.Scripts.Data;
using Il2CppAssets.Scripts.Data.Global;
using System;

namespace BloonsArchipelago.Utils
{
    public class ArchipelagoXP
    {
        public int Level = 0;
        public float XP = 0.0f;
        public long XPToNext;
        public long MaxLevel;
        public bool Maxed = false;
        public bool Curved = false;

        public ArchipelagoXP(int Level, float XP, long XPToNext, long MaxLevel, bool Curved)
        {
            this.Level = (int)Math.Min((long)Level, MaxLevel);
            this.XP = XP;
            this.XPToNext = XPToNext;
            this.MaxLevel = MaxLevel;
            this.Curved = Curved;
            Maxed = this.Level >= MaxLevel;

            if (this.Curved && !Maxed)
            {
                this.XPToNext = GameData._instance.rankInfo.GetXpDiffForRankFromPrev(Math.Max(1, this.Level));
            }
        }

        public ArchipelagoXP(long XPToNext, long MaxLevel, bool Curved)
        {
            this.XPToNext = XPToNext;
            this.MaxLevel = MaxLevel;
            this.Curved = Curved;
            this.Level = (int)Math.Min(1L, MaxLevel);
            this.Maxed = this.Level >= MaxLevel;

            if (this.Curved && !this.Maxed)
            {
                this.XPToNext = GameData._instance.rankInfo.GetXpDiffForRankFromPrev(1);
            }
        }

        public void PassXP(float XP)
        {
            this.XP += (float)Math.Round(XP);

            while (this.XP > XPToNext && Level < MaxLevel)
            {
                this.XP -= XPToNext;
                Level++;
                BloonsArchipelago.sessionHandler.CompleteCheck("Level " + Level);
                if (Curved)
                {
                    XPToNext = GameData._instance.rankInfo.GetXpDiffForRankFromPrev(Level);
                }
            }

            if (Level == MaxLevel)
            {
                Maxed = true;
            }

            BloonsArchipelago.sessionHandler.session.DataStorage["Level-" + BloonsArchipelago.sessionHandler.PlayerSlotName()] = Level;
            BloonsArchipelago.sessionHandler.session.DataStorage["XP-" + BloonsArchipelago.sessionHandler.PlayerSlotName()] = this.XP;
        }
    }
}
