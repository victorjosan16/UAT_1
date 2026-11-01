using VoxelBloom.Core;

namespace VoxelBloom
{
    /// <summary>
    /// Chooses the rewarded-ad provider. No SDK ships with the project, so production builds get
    /// NoAdsService (Double Reward shows as unavailable). Define VB_MOCK_ADS for an explicitly labelled
    /// test provider whose callbacks never grant rewards. Integrate a real SDK by adding an adapter
    /// implementing IRewardedAdService and returning it here (credentials come from your own config).
    /// </summary>
    public static class AdServiceFactory
    {
        public static IRewardedAdService Create()
        {
#if VB_MOCK_ADS
            return new MockTestAdService();
#else
            return new NoAdsService();
#endif
        }
    }
}
