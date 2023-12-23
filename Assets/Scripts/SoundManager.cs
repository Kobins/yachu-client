using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using Yachu.Server.Packets.Body;
using Random = UnityEngine.Random;

namespace Yachu.Client {
[Serializable]
public class DiceSoundByHitMaterials {
    public List<AudioClip> _weak;
    public List<AudioClip> _hard;
    public List<AudioClip> _hardest;

    [CanBeNull]
    public List<AudioClip> GetSoundsByType(int index) {
        if (index < 0 || index >= (int) DiceHitSoundType.TypeCount) {
            return null;
        }
        switch ((DiceHitSoundType)index) {
            case DiceHitSoundType.Weak: return _weak; 
            case DiceHitSoundType.Hard: return _hard; 
            case DiceHitSoundType.Hardest: return _hardest; 
        }

        return null;
    }
}
[RequireComponent(typeof(AudioSource))]
public class SoundManager : MonoSingleton<SoundManager> {

    private const string MusicVolumeKey = "volume_music";
    private const string SoundVolumeKey = "volume_sound";

    [Header("Music")]
    private AudioSource _audioSource;
    public float MusicVolume {
        get => _audioSource.volume;
        set {
            _audioSource.volume = value;
            PlayerPrefs.SetFloat(MusicVolumeKey, value);
        }
    }

    private float _soundVolume = 0.5f;
    public float SoundVolume { 
        get => _soundVolume;
        set {
            _soundVolume = value;
            PlayerPrefs.SetFloat(SoundVolumeKey, value);
        } 
    }
    [SerializeField] private AudioClip _cupShakingMusic;
    [SerializeField] private AudioClip _selectingMusic;
    
    [Header("SFX")] 
    [SerializeField] private AudioClip _selectSound;
    public AudioClip SelectSound => _selectSound;
    
    [SerializeField] private AudioClip _diceKeepSound;
    public AudioClip DiceKeepToggleSound => _diceKeepSound;
    
    [SerializeField] private AudioClip _scoreMarkSoundNormal;
    [SerializeField] private AudioClip _scoreMarkSoundSpecial;
    [SerializeField] private AudioClip _scoreMarkSoundZero;
    public enum ScoreMarkType {
        Normal = 0,
        Special,
        Zero,
        TypeCount
    }
    public AudioClip GetScoreMarkSound(ScoreMarkType type) {
        switch (type) {
            case ScoreMarkType.Normal: return _scoreMarkSoundNormal;
            case ScoreMarkType.Special: return _scoreMarkSoundSpecial;
            case ScoreMarkType.Zero: return _scoreMarkSoundZero;
        }

        return null;
    }
    
    [SerializeField] private AudioClip _diceDeterminedSound;
    [SerializeField] private AudioClip _diceDeterminedSoundMade;
    public AudioClip GetDiceDeterminedSound(bool made) => made ? _diceDeterminedSoundMade : _diceDeterminedSound;
    
    [SerializeField] private AudioClip _diceThrowSound;
    public AudioClip DiceThrowSound => _diceThrowSound;
    
    [Header("Dice Hit Sounds")]
    [SerializeField] private DiceSoundByHitMaterials _diceToCupSound;
    [SerializeField] private DiceSoundByHitMaterials _diceToDiceSound;
    [SerializeField] private DiceSoundByHitMaterials _diceToFloorSound;
    [SerializeField] private DiceSoundByHitMaterials _diceToWallSound;

    private List<List<List<AudioClip>>> _diceSounds;
    [CanBeNull]
    public AudioClip this[DiceHitMaterialType diceHitType, DiceHitSoundType diceHitSoundType] {
        get {
            var sounds = _diceSounds[(int) diceHitType][(int) diceHitSoundType];
            var index = Random.Range(0, sounds.Count);
            return sounds[index];
        }
    }

    [CanBeNull]
    private DiceSoundByHitMaterials GetSoundsByHitMaterials(int index) {
        if (index < 0 || index >= (int) DiceHitMaterialType.TypeCount) {
            return null;
        }
        switch ((DiceHitMaterialType)index) {
            case DiceHitMaterialType.Cup: return _diceToCupSound; 
            case DiceHitMaterialType.Dice: return _diceToDiceSound; 
            case DiceHitMaterialType.BoardFloor: return _diceToFloorSound; 
            case DiceHitMaterialType.BoardWall: return _diceToWallSound; 
        }

        return null;
    }
    protected override void OnAwake() {
        const int hitMaterialCount = (int) DiceHitMaterialType.TypeCount;
        _diceSounds = new List<List<List<AudioClip>>>(hitMaterialCount);
        for (int h = 0; h < hitMaterialCount; ++h) {
            var soundsByHit = GetSoundsByHitMaterials(h);
            if (soundsByHit == null) {
                Debug.Log($"Cannot load dice hit sound with material {(DiceHitMaterialType)h}");
                continue;
            }
            
            const int soundTypeCount = (int) DiceHitSoundType.TypeCount;
            _diceSounds.Add(new List<List<AudioClip>>(soundTypeCount));
            
            for (int t = 0; t < soundTypeCount; ++t) {
                var soundsByType = soundsByHit.GetSoundsByType(t);
                if (soundsByType == null) {
                    Debug.Log($"Cannot load dice hit sound with type {(DiceHitSoundType)t}");
                    continue;
                }

                _diceSounds[h].Add(soundsByType);
            }
        }

        _audioSource = GetComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        MusicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 0.5f);
        SoundVolume = PlayerPrefs.GetFloat(SoundVolumeKey, 0.5f);
    }

    public void PlaySound(AudioClip sound) {
        _audioSource.PlayOneShot(sound, SoundVolume);
    }

    public void PlayCupShakingMusic() {
        _audioSource.Stop();
        _audioSource.clip = _cupShakingMusic;
        _audioSource.Play();
    }

    public void PlaySelectingMusic(bool made) {
        _audioSource.Stop();
        _audioSource.clip = _selectingMusic;
        StartCoroutine(PlayAfter(made ? 3f : 1f));
        PlaySound(GetDiceDeterminedSound(made));
    }

    private IEnumerator PlayAfter(float time) {
        yield return new WaitForSeconds(time);
        _audioSource.Play();
    }
    
    public void StopMusic() {
        _audioSource.Stop();
        _audioSource.clip = null;
    }

}
}