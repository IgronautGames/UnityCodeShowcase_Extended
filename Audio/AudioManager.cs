using ClumsyCrew.Characters;
using ClumsyCrew.Core;
using ClumsyCrew.Minigames;
using ClumsyCrew.UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using static ClumsyCrew.GameEnums;

namespace ClumsyCrew.Audio
{
    public class AudioManager : MonoBehaviour
    {
        [Header("Mixer")]
        [SerializeField] AudioMixer mixer;
        [SerializeField] AudioMixerGroup musicGroup;
        [SerializeField] AudioMixerGroup sfxGroup;
        [SerializeField] AudioMixerGroup uiGroup;
        [SerializeField] AudioMixerGroup voCharactersGroup;
        [SerializeField] AudioMixerGroup voCommentatorGroup;
        [SerializeField] AudioMixerGroup ambienceGroup;
        [Header("Mixer Params (Exposed)")]
        [SerializeField] string masterVolumeParam = "MasterVolume";
        [SerializeField] string musicVolumeParam = "MusicVolume";
        [SerializeField] string soundsVolumeParam = "SoundsVolume";  // can drive SFX + ambience
        [SerializeField] string uiVolumeParam = "UIVolume";
        [SerializeField] string voVolumeParam = "VOVolume";
        [Header("Music")]
        [SerializeField] float defaultMusicFadeDuration = 1f;
        [SerializeField] AudioListener listener;
        [SerializeField] MetaEventsRouter metaRouter;
        [SerializeField] UIEventsRouter uiRouter;
        [SerializeField] MinigameEventsRouter minigameRouter;

        AudioSource musicA;
        AudioSource musicB;
        bool musicToggle;

        Dictionary<int, AudioSource> charVoSources = new();
        Dictionary<SoundID, AudioSource> ambienceSources = new();
        AudioSource commentatorVoSource;
        Dictionary<SoundID, float> lastPlayTime = new();
        Dictionary<SoundID, int> activeInstances = new();

        [Header("SFX Pool")]
        [SerializeField] int poolSize = 48;
        List<AudioSource> sfxPool = new();
        int nextSfxIndex;

        float masterVolume;
        float musicVolume;
        float soundsVolume;
        float uiVolume;
        float voVolume;

        // TODO: Use playRealSpatials to gate spatialBlend in solo and local multipley vs online multiplayer
        bool playRealSpatials;

        LanguageType currentLanguage = LanguageType.English;

        IReadOnlyDictionary<SoundID, SoundDefinition> soundDefsDict;

        #region Init
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
        private void Start()
        {
            soundDefsDict = DefinitionsManager.Instance.SoundDefsDict;

            InitializeMusicSources();
            InitializeSfxPool();
            InitializeVOSoruce();
            AddListeners();

            masterVolume = PlayerPrefs.GetFloat("masterVol", 1f);
            musicVolume = PlayerPrefs.GetFloat("musicVol", 1f);
            soundsVolume = PlayerPrefs.GetFloat("soundVol", 1f);
            uiVolume = PlayerPrefs.GetFloat("uiVol", 1f);
            voVolume = PlayerPrefs.GetFloat("voVol", 1f);

            MasterVolume(masterVolume);
            MusicVolume(musicVolume);
            SoundsVolume(soundsVolume);
            UIVolume(uiVolume);
            VOVolume(voVolume);
        }


        void InitializeMusicSources()
        {
            musicA = CreateChildAudioSource("MusicA", musicGroup, default, true);
            musicB = CreateChildAudioSource("Music_B", musicGroup, default, true);
        }
        void InitializeSfxPool()
        {
            for (int i = 0; i < poolSize; i++)
            {
                var src = CreateChildAudioSource($"SFX{i}", sfxGroup);
                sfxPool.Add(src);
            }
        }
        void InitializeVOSoruce()
        {
            commentatorVoSource = CreateChildAudioSource("CommentatorVO", voCommentatorGroup);
        }
        AudioSource CreateChildAudioSource(string name, AudioMixerGroup group, Transform parent = default, bool loop = false, float spatialBlend = 0, bool playOnAwake = false)
        {
            GameObject go = new (name);
            go.transform.SetParent(parent? transform : parent, false);

            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = playOnAwake;
            src.loop = loop;
            src.spatialBlend = spatialBlend;
            src.outputAudioMixerGroup = group;

            return src;
        }
        #endregion


        #region Listners
        void AddListeners()
        {
            uiRouter.OnVolumeChanged += OnVolumeChanged;
            uiRouter.OnSound += OnSound;
            uiRouter.OnSoundDefinition += OnSoundDefinition;
            metaRouter.OnSound += OnSound;
            minigameRouter.OnCommentaryClip += PlayCommentatorVo;
            minigameRouter.OnAmbience += OnAmbience;
            minigameRouter.OnSound += OnSound;
            minigameRouter.OnSoundDefinition += OnSoundDefinition;

            GameManager.Instance.GameStateChanged += GameStateChanged;
            CharactersManager.Instance.OnCharacterSpawned += OnCharacterSpawned;
            if (PlatformManager.CurrentGrupped == GamePlatform.Mobile)
                AdsManager.Instance.OnWatching += OnAds;
        }
        void GameStateChanged(GameState state)
        {
            if (state == GameState.MainSceneEntered)
            {
                OnSound(SoundID.MusicMenu);
            }
            if (state == GameState.MinigameEntered)
            {
                StopMusic();
                if (GameManager.Instance.CurrentMinigameDef.MinigameType == MinigameType.LocalMultiplayer)
                {
                    playRealSpatials = true;
                    listener.enabled = true;
                }
                else
                {
                    playRealSpatials = false;
                    listener.enabled = false;
                }
            }
            else if (state == GameState.MinigameEnd)
                StopMusic();
            else if (state == GameState.ExitingMinigame)
                StopAllAmbience();
            else if (state == GameState.Menu)
            {
                foreach (var src in charVoSources.Values)
                    Destroy(src.gameObject);
                charVoSources.Clear();
                listener.enabled = true;
                OnSound(SoundID.MusicMenu);
            }
        }
        void OnCharacterSpawned(CharacterScript character)
        {
            CreateOrGetVOAudioSource(character.Index);

            character.AudioRouter.OnSound += OnSound;
            character.AudioRouter.OnVO += PlayVOSource;
        }
        void OnSound(SoundID id)
        {
            SoundDefinition def = GetSoundDef(id);
            if (def == null) return;

            OnSoundDefinition(def);
        }
        void OnSoundDefinition(SoundDefinition def)
        {
            if (def.useLimits)
            {
                if (lastPlayTime.TryGetValue(def.id, out float lastTime))
                    if (Time.unscaledTime - lastTime < def.cooldown)
                        return;

                if (activeInstances.TryGetValue(def.id, out int count))
                    if (count >= def.maxInstances)
                        return;
            }
            switch (def.type)
            {
                case SoundType.Music:
                    PlayMusic(def);
                    break;
                case SoundType.Environment:
                    PlayAmbience(def.id);
                    break;
                default:
                    PlaySFX(def, null);
                    break;
            }
        }
        void OnAds(bool ads)
        {
            MasterVolume(ads ? 0 : PlayerPrefs.GetFloat("masterVol"));
        }
        void OnAmbience(SoundID id, float fading, bool play)
        {
            if (play)
                PlayAmbience(id, fading);
            else
                StopAmbience(id, fading);
        }
        #endregion



        #region Playing
        // ------- MUSIC -------
        void PlayMusic(SoundDefinition def, float duration = 1f)
        {
            AudioSource target = musicToggle ? musicB : musicA;
            AudioSource other = musicToggle ? musicA : musicB;

            musicToggle = !musicToggle;

            SoundClipData clipToPlay = def.clips.GetRandom();
            if (!SetSoundToSource(target, clipToPlay, def)) return;
            target.volume = 0;

            FadeInOutSource(target, true, clipToPlay.volumeMinMax.GetRandomRange(), duration);
            FadeInOutSource(other, false, 0, duration);
        }
        void StopMusic(float fadeDuration = 0.5f)
        {
            FadeInOutSource(musicA, false, 0, fadeDuration);
            FadeInOutSource(musicB, false, 0, fadeDuration);
        }
        // ------- SFX / AMBIENT / UI -------
        void PlaySFX(SoundID id)
        {
            SoundDefinition def = GetSoundDef(id);
            if (def == null) return;

            PlaySFX(def, null);
        }
        void PlaySFX(SoundDefinition def, Vector3? worldPos)
        {
            AudioSource src = GetFreeSfxSource();
            if (src == null)
            {
                Debug.LogWarning("No free source for SFX");
                return;
            }
            src.outputAudioMixerGroup = def.type switch
            {
                SoundType.Meta or SoundType.Notifications or SoundType.GameplayObjects or SoundType.ToolsWeapons or SoundType.CharacterFoley => sfxGroup,
                SoundType.CrowdReactions or SoundType.Environment => ambienceGroup,
                SoundType.UI => uiGroup,
                _ => sfxGroup,
            };

            SoundClipData clipToPlay = def.clips.GetRandom();
            if (!SetSoundToSource(src, clipToPlay, def)) return;

            // TODO: Support world-position SFX (weapon hits, job item destruction, explosions) - when create online multiplayer - before that no needed

            if (def.spatial && worldPos.HasValue)
            {
                src.transform.position = worldPos.Value;
                src.spatialBlend = def.spatialBlend;
            }
            else
            {
                src.transform.position = Vector3.zero;
                src.spatialBlend = 0f;
            }


            src.Play();
            if (def.useLimits)
            {
                lastPlayTime[def.id] = Time.unscaledTime;
                activeInstances[def.id]++;
                LeanTween.delayedCall(clipToPlay.clip.length, () =>
                {
                    activeInstances[def.id] = Mathf.Max(0, activeInstances[def.id] - 1);
                });
            }
        }
        void PlayAmbience(SoundID id, float fadeDuration = 1f)
        {
            SoundDefinition def = GetSoundDef(id);
            if (def == null) return;

            AudioSource src = CreateOrGetAmbienceSource(def);
            SoundClipData clipToPlay = def.clips.GetRandom();
            if (!SetSoundToSource(src, clipToPlay, def))return;

            src.volume = 0;
            FadeInOutSource(src, true, clipToPlay.volumeMinMax.GetRandomRange(), fadeDuration);
        }
        void StopAmbience(SoundID id, float fadeDuration = 1f)
        {
            if (!ambienceSources.TryGetValue(id, out var src))
                return;

            FadeInOutSource(src, false, 0, fadeDuration, () =>
            {
                src.Stop();
            });
        }
        public void StopAllAmbience(float fadeDuration = 1f)
        {
            foreach (var src in ambienceSources.Values)
            {
                FadeInOutSource(src, false, soundsVolume, fadeDuration, () =>
                {
                    src.Stop();
                });
            }
        }
        // ------- VO (called from VO systems) -------
        void PlayVOSource(int voIndex, SoundDefinition def)
        {
            if (def.localizedClips == null)
            {
                Debug.Log("No localized clips at all for " + def.id + " for character " + def.character);
                return;
            }
            SoundClipData clip;
            if (def.localizedClips[currentLanguage] != null && def.localizedClips[currentLanguage].Count > 0)
                clip = def.localizedClips[currentLanguage].GetRandom();
            else
            {
                Debug.Log("No " + def.id + " VO clip for " + currentLanguage + " language");
                if (currentLanguage != LanguageType.English)
                {
                    if (def.localizedClips[LanguageType.English].Count > 0)
                        clip = def.localizedClips[LanguageType.English].GetRandom();
                    else
                        Debug.Log("Nether default VO");
                    return;
                }
                else return;
            }

            AudioSource voSource = CreateOrGetVOAudioSource(voIndex);
            voSource.Stop();
            if (!SetSoundToSource(voSource, clip, def));
            voSource.Play();
        }
        void PlayCommentatorVo(SoundClipData clip, SoundDefinition def)
        {
            commentatorVoSource.Stop();
            if (!SetSoundToSource(commentatorVoSource, clip, def));
            commentatorVoSource.Play();
        }
        #endregion


        #region Fades / Volumes
        void FadeInOutSource(AudioSource source, bool fadeIn, float maxVolume, float duration = 0.5f, UnityAction OnDoneAction = default)
        {
            float from = fadeIn ? 0f : source.volume;
            float to = fadeIn ? maxVolume : 0f;

            if (fadeIn && !source.isPlaying)
                source.Play();

            LeanTween.value(gameObject, from, to, duration).setIgnoreTimeScale(true).setOnUpdate((float value) =>
            {
                source.volume = value;
            }).setOnComplete(() =>
            {
                if (!fadeIn)
                    source.Pause();
                OnDoneAction?.Invoke();
            });
        }
        void OnVolumeChanged(AudioVolumeGroup group, float value)
        {
            switch (group)
            {
                case AudioVolumeGroup.Master:
                    MasterVolume(value);
                    break;
                case AudioVolumeGroup.Music:
                    MusicVolume(value);
                    break;
                case AudioVolumeGroup.Sounds:
                    SoundsVolume(value);
                    break;
                case AudioVolumeGroup.UI:
                    UIVolume(value);
                    break;
                case AudioVolumeGroup.VO:
                    VOVolume(value);
                    break;
            }
        }
        void MasterVolume(float value)
        {
            masterVolume = value;
            PlayerPrefs.SetFloat("masterVol", value);
            SetMixerVolume(masterVolumeParam, value);
        }
        void MusicVolume(float value)
        {
            musicVolume = value;
            PlayerPrefs.SetFloat("musicVol", value);
            SetMixerVolume(musicVolumeParam, value);
        }
        void SoundsVolume(float value)
        {
            soundsVolume = value;
            PlayerPrefs.SetFloat("soundVol", value);
            SetMixerVolume(soundsVolumeParam, value);
        }
        void UIVolume(float value)
        {
            uiVolume = value;
            PlayerPrefs.SetFloat("uiVol", value);
            SetMixerVolume(uiVolumeParam, value);
        }
        void VOVolume(float value)
        {
            voVolume = value;
            PlayerPrefs.SetFloat("voVol", value);
            SetMixerVolume(voVolumeParam, value);
        }
        void SetMixerVolume(string param, float value)
        {
            if (mixer == null || string.IsNullOrEmpty(param))
                return;

            value = Mathf.Clamp(value, 0.0001f, 1f);   // avoid log10(0)
            float dB = Mathf.Log10(value) * 20f;       // map 0–1 to -80..0 dB-ish
            mixer.SetFloat(param, dB);
        }
        #endregion


        #region Other
        SoundDefinition GetSoundDef(SoundID id)
        {
            SoundDefinition def;
            if (soundDefsDict.ContainsKey(id))
                def = soundDefsDict[id];
            else
            {
                Debug.LogWarning("SoundDefinitionDict does not contains " + id.ToString());
                return null;
            }
            if (!def.clips.IsNotNullAndHasElements())
            {
                Debug.LogWarning("SoundDefinition " + id.ToString() + " does not have clips");
                return null;
            }
            return def;
        }
        AudioSource GetFreeSfxSource()
        {
            int start = nextSfxIndex;

            for (int i = 0; i < sfxPool.Count; i++)
            {
                int index = (start + i) % sfxPool.Count;
                var src = sfxPool[index];

                if (!src.isPlaying)
                {
                    nextSfxIndex = (index + 1) % sfxPool.Count;
                    return src;
                }
            }

            // All playing, steal next one (very rare, but safe fallback)
            var fallback = sfxPool[nextSfxIndex];
            fallback.Stop();
            // We don't know which SoundID was playing, so we don't decrement.
            // This only happens in extreme overload situations.
            nextSfxIndex = (nextSfxIndex + 1) % sfxPool.Count;
            return fallback;
        }
        AudioSource CreateOrGetVOAudioSource(int index)
        {
            if (!charVoSources.ContainsKey(index))
                charVoSources.Add(index, CreateChildAudioSource("Player" + index, voCharactersGroup));
            return charVoSources[index];
        }
        AudioSource CreateOrGetAmbienceSource(SoundDefinition def)
        {
            SoundID id = def.id;
            if (!ambienceSources.ContainsKey(id))
            {
                ambienceSources.Add(id, CreateChildAudioSource("Ambience_" + id, ambienceGroup));
                ambienceSources[id].loop = def.loop;
            }
            return ambienceSources[id];
        }
        bool SetSoundToSource(AudioSource src, SoundClipData clip, SoundDefinition def)
        {
            if (src == null)
            {
                Debug.Log("Source for clip " + clip.clip.name + " does not exist");
                return false;
            }
            else if (clip == null)
            {
                Debug.Log("Clip data from " + def.id + " does not exist");
                return false;
            }
            else if (clip.clip == null)
            {
                Debug.Log("Clip from " + def.id + " does not exist");
                return false;
            }
            else if (def == null)
            {
                Debug.Log("SoundDefinition for clip " + clip.clip.name + " does not exist");
                return false;
            }
            src.clip = clip.clip;
            src.volume = clip.volumeMinMax.GetRandomRange();
            src.pitch = clip.pitchMinMax.GetRandomRange();
            src.loop = def.loop;
            src.spatialBlend = def.spatial ? def.spatialBlend : 0f;
            return true;
        }
        #endregion
    }
}

