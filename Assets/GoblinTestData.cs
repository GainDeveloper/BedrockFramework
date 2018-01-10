using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Goblin Test Config", menuName = "Configs/Goblin Test", order = 1)]
public class GoblinTestData : Prototype {
	public GameObject asset;
	public float health;
	public int damage;
    public SoundInfo attackSound;

    [System.Serializable]
    public class SoundInfo
    {
        public AudioClip clip;
        public float volume = 1;
    }
}
