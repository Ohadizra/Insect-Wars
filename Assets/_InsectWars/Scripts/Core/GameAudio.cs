using InsectWars.Data;
using UnityEngine;

namespace InsectWars.Core
{
    /// <summary>
    /// Optional one-shot UI/combat clips. Assign on the GameAudio component in the scene (or leave empty for silent play).
    /// </summary>
    public class GameAudio : MonoBehaviour
    {
        public enum UiKind { PauseToggle, MatchVictory, MatchDefeat, Click }

        public static GameAudio Instance { get; private set; }

        [SerializeField] AudioClip pauseToggle;
        [SerializeField] AudioClip matchVictory;
        [SerializeField] AudioClip matchDefeat;
        [SerializeField] AudioClip uiClick;
        [SerializeField] AudioClip combatHit;
        [SerializeField] public AudioClip constructionComplete;
        [SerializeField] public AudioClip workerLift;
        [SerializeField] AudioClip backgroundMusic;
        [SerializeField] AudioClip attackWood;
        [SerializeField] AudioClip attackScratch;
        [SerializeField] AudioClip attackBone;
        [SerializeField] AudioClip attackSpray;
        [SerializeField] [Range(0f, 1f)] float uiVolume = 0.65f;
        [SerializeField] [Range(0f, 1f)] float combatVolume = 0.5f;
        [SerializeField] [Range(0f, 1f)] float musicVolume = 0.4f;

        AudioSource _src;
        AudioSource _musicSrc;

        void Awake()
        {
            Instance = this;
            _src = GetComponent<AudioSource>();
            if (_src == null)
            {
                _src = gameObject.AddComponent<AudioSource>();
                _src.playOnAwake = false;
                _src.spatialBlend = 0f;
            }

            _musicSrc = gameObject.AddComponent<AudioSource>();
            _musicSrc.loop = true;
            _musicSrc.playOnAwake = false;
            _musicSrc.spatialBlend = 0f;
        }

        void OnDestroy()
{
            if (Instance == this) Instance = null;
        }

        public static void PlayUi(UiKind kind)
        {
            if (Instance == null) return;
            var clip = kind switch
            {
                UiKind.PauseToggle => Instance.pauseToggle,
                UiKind.MatchVictory => Instance.matchVictory,
                UiKind.MatchDefeat => Instance.matchDefeat,
                UiKind.Click => Instance.uiClick,
                _ => null
            };
            if (clip != null && Instance._src != null)
                Instance._src.PlayOneShot(clip, Instance.uiVolume);
        }

        public static void PlayWorld(AudioClip clip, Vector3 world, float volumeScale = 1f)
        {
            if (clip == null || Instance == null || Instance._src == null) return;
            AudioSource.PlayClipAtPoint(clip, world, Instance.uiVolume * volumeScale);
        }

        public static void PlayCombatHit(Vector3 world)
        {
            if (Instance == null || Instance.combatHit == null) return;
            AudioSource.PlayClipAtPoint(Instance.combatHit, world, Instance.combatVolume);
        }

        public static void PlayWorkerLift()
        {
            if (Instance == null) return;
            if (Instance.workerLift == null)
                Instance.workerLift = Resources.Load<AudioClip>("Audio/Worker_Lift");
            
            if (Instance.workerLift != null)
                Instance._src.PlayOneShot(Instance.workerLift, Instance.uiVolume);
        }

        public static void PlayMenuMusic(float fadeDuration = 0f)
        {
            if (Instance == null) return;
            
            var menuClip = Resources.Load<AudioClip>("Audio/Music_MenuAmbient");
            if (menuClip == null) return;

            if (Instance._musicSrc != null)
            {
                if (Instance._musicSrc.clip == menuClip && Instance._musicSrc.isPlaying)
                    return;

                Instance._musicSrc.clip = menuClip;
                Instance._musicSrc.loop = true;
                Instance.backgroundMusic = menuClip;

                if (fadeDuration > 0f)
                {
                    Instance._musicSrc.volume = 0f;
                    Instance._musicSrc.Play();
                    Instance.StartCoroutine(Instance.FadeInMusic(fadeDuration));
                }
                else
                {
                    Instance._musicSrc.volume = Instance.musicVolume;
                    Instance._musicSrc.Play();
                }
            }
        }

        System.Collections.IEnumerator FadeInMusic(float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                _musicSrc.volume = Mathf.Lerp(0f, musicVolume, t / duration);
                yield return null;
            }
            _musicSrc.volume = musicVolume;
        }

        public static void PlayGameMusic()
        {
            if (Instance == null) return;
            
            var theme = ScatterTheme.Default;
            if (GameSession.SelectedMap != null) theme = GameSession.SelectedMap.scatterTheme;

            string path = theme switch
            {
                ScatterTheme.Frozen => "Audio/Music_TribalBattle",
                ScatterTheme.Lava => "Audio/Music_LavaPass",
                _ => "Audio/Music_TribalBattle"
            };

            var clip = Resources.Load<AudioClip>(path);
            if (clip != null && Instance._musicSrc != null)
            {
                Instance._musicSrc.clip = clip;
                Instance._musicSrc.volume = Instance.musicVolume;
                Instance._musicSrc.loop = true;
                Instance._musicSrc.Play();
                Instance.backgroundMusic = clip; // Update cache
            }
        }

        public static void PlayAttack(UnitArchetype arch, Vector3 pos)
        {
            if (Instance == null) return;
            AudioClip clip = arch switch
            {
                UnitArchetype.Worker => Instance.LoadClip(ref Instance.attackWood, "Audio/Attack_WoodClash"),
                UnitArchetype.BasicFighter => Instance.LoadClip(ref Instance.attackWood, "Audio/Attack_WoodClash"),
                UnitArchetype.BlackWidow => Instance.LoadClip(ref Instance.attackScratch, "Audio/Attack_Scratch"),
                UnitArchetype.GiantStagBeetle => Instance.LoadClip(ref Instance.attackBone, "Audio/Attack_BoneCrunch"),
                UnitArchetype.BasicRanged => Instance.LoadClip(ref Instance.attackSpray, "Audio/Attack_SprayHiss"),
                _ => null
            };
            if (clip != null)
                AudioSource.PlayClipAtPoint(clip, pos, Instance.combatVolume);
        }

        AudioClip LoadClip(ref AudioClip field, string path)
        {
            if (field == null) field = Resources.Load<AudioClip>(path);
            return field;
        }
        }
        }
