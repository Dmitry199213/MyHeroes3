using System.Collections;
using UnityEngine;

namespace HeroesBattle
{
    /// <summary>Точка входа: меню, выбор армии, запуск боя, результаты.</summary>
    public class GameRoot : MonoBehaviour
    {
        Camera cam;
        GameObject screenRoot;
        GameObject battleRoot;
        BattleHUD currentHud;
        ArmySetup lastSetup;
        int battlesStarted;
        bool battleFinished;

        public bool AutoPlay { get; set; }

        void Awake()
        {
            Application.targetFrameRate = 60;
            SetupCamera();
            Sfx.Init();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.M)) Sfx.ToggleMute();
        }

        void Start()
        {
            var args = System.Environment.GetCommandLineArgs();

            if (HasArg(args, "-shotresult"))
            {
                AutoPlay = true;
                Time.timeScale = 3f;
                StartRandomBattle();
                StartCoroutine(ShotResult());
                return;
            }

            if (HasArg(args, "-shotbattle"))
            {
                StartRandomBattle();
                StartCoroutine(ShotAfter("shot_battle.png", 1.6f));
                return;
            }

            if (HasArg(args, "-shotselect"))
            {
                ShowArmySelect();
                StartCoroutine(ShotAfter("shot_select.png", 1.0f));
                return;
            }

            if (HasArg(args, "-shotmenu"))
            {
                ShowMenu();
                StartCoroutine(ShotAfter("shot_menu.png", 0.9f));
                return;
            }

            ShowMenu();
        }

        static bool HasArg(string[] args, string name)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == name) return true;
            }

            return false;
        }

        void SetupCamera()
        {
            cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
            }

            cam.orthographic = true;
            cam.orthographicSize = 9f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.035f, 0.03f);
            cam.transform.position = new Vector3(8.6f, -5.2f, -10f);

            if (cam.GetComponent<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();
        }

        // ------------------------------------------------------------------ screens

        public void ShowMenu()
        {
            ClearScreens();

            var go = new GameObject("MenuScreen");
            var menu = go.AddComponent<MainMenuUI>();
            menu.OnNewBattle = ShowArmySelect;
            menu.OnRandomBattle = StartRandomBattle;
            menu.OnQuit = QuitGame;
            menu.Build();

            screenRoot = go;
        }

        public void ShowArmySelect()
        {
            ClearScreens();

            var go = new GameObject("ArmySelectScreen");
            var select = go.AddComponent<ArmySelectUI>();
            select.OnBack = ShowMenu;
            select.OnStart = StartBattle;
            select.Build();

            screenRoot = go;
        }

        void StartRandomBattle()
        {
            var rng = new System.Random();
            var army = ArmyBuilder.RandomPlayerArmy(rng);
            var hero = HeroDb.PlayerHeroes[rng.Next(HeroDb.PlayerHeroes.Length)];
            var setup = ArmyBuilder.CreateSetup(army, hero, Difficulty.Normal, rng);
            StartBattle(setup);
        }

        public void StartBattle(ArmySetup setup)
        {
            if (setup == null) return;

            ClearScreens();

            if (setup.EnemyArmy == null || setup.EnemyArmy.Count == 0)
            {
                var rng = new System.Random();
                var hero = setup.PlayerHero != null ? setup.PlayerHero : HeroDb.PlayerHeroes[0];
                var generated = ArmyBuilder.CreateSetup(setup.PlayerArmy, hero, setup.Difficulty, rng);
                setup.EnemyArmy = generated.EnemyArmy;
                setup.EnemyHero = generated.EnemyHero;
            }

            lastSetup = setup;
            battlesStarted++;
            battleFinished = false;
            Sfx.Play(SfxId.Turn, 0.5f);

            battleRoot = new GameObject("BattleScreen");

            int seed = new System.Random().Next();
            var state = BattleState.Create(setup, seed);

            var view = battleRoot.AddComponent<BattleView>();
            view.Build(state);

            var hud = battleRoot.AddComponent<BattleHUD>();
            hud.Build(state);
            hud.OnRestart = ReplayBattle;
            hud.OnMenu = ShowMenu;

            currentHud = hud;

            var controller = battleRoot.AddComponent<BattleController>();
            controller.Init(state, view, hud, this);
        }

        void ReplayBattle()
        {
            if (lastSetup == null)
            {
                ShowMenu();
                return;
            }

            StartBattle(lastSetup);
        }

        public void OnBattleFinished(bool playerWon)
        {
            battleFinished = true;
            if (currentHud != null) currentHud.ShowResult(playerWon);
        }

        void ClearScreens()
        {
            if (screenRoot != null) Destroy(screenRoot);
            if (battleRoot != null) Destroy(battleRoot);
            screenRoot = null;
            battleRoot = null;
            currentHud = null;
        }

        void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ------------------------------------------------------------------ screenshots (проверка рендера)

        IEnumerator ShotAfter(string fileName, float delay)
        {
            yield return new WaitForSeconds(delay);
            CaptureShot(fileName);
            yield return new WaitForSeconds(0.8f);
            Application.Quit();
        }

        IEnumerator ShotResult()
        {
            float elapsed = 0f;
            while (!battleFinished && elapsed < 150f)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Debug.Log("[HeroesBattle] autoplay finished=" + battleFinished + " in " + elapsed.ToString("0.0") + "s");
            yield return new WaitForSecondsRealtime(0.6f);
            CaptureShot("shot_result.png");
            yield return new WaitForSecondsRealtime(1.0f);
            Application.Quit();
        }

        static string CaptureShot(string fileName)
        {
            string dir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "Screenshots"));
            System.IO.Directory.CreateDirectory(dir);
            string path = System.IO.Path.Combine(dir, fileName);
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log("[HeroesBattle] screenshot: " + path);
            return path;
        }
    }
}
