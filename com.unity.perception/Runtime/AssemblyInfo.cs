using System.Runtime.CompilerServices;

#if UNITY_EDITOR
[assembly: InternalsVisibleTo("Unity.Perception.Editor.Tests")]
[assembly: InternalsVisibleTo("Unity.Perception.Editor")]
#endif
[assembly: InternalsVisibleTo("Untiy.Perception.Tests.Scripts")]
[assembly: InternalsVisibleTo("Unity.Perception.Runtime.Tests")]
[assembly: InternalsVisibleTo("Unity.Perception.Runtime")]
[assembly: InternalsVisibleTo("Unity.Perception.TestProject")]
[assembly: InternalsVisibleTo("Unity.Perception.Performance.Tests")]
