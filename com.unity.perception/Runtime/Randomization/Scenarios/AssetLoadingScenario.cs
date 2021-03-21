// using UnityEngine.AddressableAssets;
//
// namespace UnityEngine.Perception.Randomization.Scenarios
// {
//     [AddComponentMenu("Perception/Scenarios/Asset Loading Scenario")]
//     public class AssetLoadingScenario : FixedLengthScenario
//     {
//         bool m_AssetsLoaded;
//
//
//         protected override bool isScenarioReadyToStart => m_AssetsLoaded;
//
//         protected override void OnStart()
//         {
//             base.OnStart();
//             Addressables.LoadAssetsAsync<GameObject>("", null).Completed += handle =>
//             {
//
//             };
//         }
//     }
// }
