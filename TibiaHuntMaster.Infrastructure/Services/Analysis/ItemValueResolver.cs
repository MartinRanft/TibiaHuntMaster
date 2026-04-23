namespace TibiaHuntMaster.Infrastructure.Services.Analysis
{
    public static class ItemValueResolver
    {
        public static long GetEffectiveValue(long storedValue, long? npcValue, long? npcPrice)
        {
            if(storedValue > 0)
            {
                return storedValue;
            }

            if(npcValue.GetValueOrDefault() > 0)
            {
                return npcValue!.Value;
            }

            if(npcPrice.GetValueOrDefault() > 0)
            {
                return npcPrice!.Value;
            }

            return 0;
        }

        public static long GetEffectiveValue(long? storedValue, long? npcValue, long? npcPrice)
        {
            return GetEffectiveValue(storedValue.GetValueOrDefault(), npcValue, npcPrice);
        }
    }
}
