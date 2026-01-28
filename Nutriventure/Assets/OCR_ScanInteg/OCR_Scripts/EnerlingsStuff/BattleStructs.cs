using System.Collections.Generic;

public static class BattleStructs
{
    public struct DamageBreakdown
    {
        public int baseDamage;
        public List<FeedbackManager.OrganBonus> organBonuses;
        public int totalDamage;

        public DamageBreakdown(int baseDamage, List<FeedbackManager.OrganBonus> organBonuses)
        {
            this.baseDamage = baseDamage;
            this.organBonuses = organBonuses;
            this.totalDamage = baseDamage;

            if (organBonuses != null)
            {
                foreach (var bonus in organBonuses)
                {
                    this.totalDamage += bonus.bonusAmount;
                }
            }
        }
    }

    public struct HealBreakdown
    {
        public int baseHeal;
        public List<FeedbackManager.OrganBonus> organBonuses;
        public int totalHeal;

        public HealBreakdown(int baseHeal, List<FeedbackManager.OrganBonus> organBonuses)
        {
            this.baseHeal = baseHeal;
            this.organBonuses = organBonuses;
            this.totalHeal = baseHeal;

            if (organBonuses != null)
            {
                foreach (var bonus in organBonuses)
                {
                    this.totalHeal += bonus.bonusAmount;
                }
            }
        }
    }
}