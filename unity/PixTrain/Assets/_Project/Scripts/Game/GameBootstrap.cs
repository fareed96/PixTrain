using UnityEngine;

namespace PixTrain.Game
{
    /// <summary>
    /// Sahne içeriği koddan kurulur: hangi sahne açılırsa açılsın oyunu bir kez başlatır.
    /// Böylece sahne dosyası boş kalır ve editör olmadan da proje yönetilebilir.
    /// </summary>
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (Object.FindAnyObjectByType<GameController>() != null) return;
            var go = new GameObject("PixTrain");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<GameController>();
        }
    }
}
