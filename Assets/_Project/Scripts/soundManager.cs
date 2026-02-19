using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class soundManager : MonoBehaviour
{
    public AudioSource SoundSource;
    public AudioClip clickSound;
    public AudioClip winSound;
    public AudioClip loseSound;
    public AudioClip newFloorSound;
    public AudioClip explosionSound;

    public static soundManager Instance;

    private void Awake()
    {
        Instance = this;
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void playSoundEffect (AudioClip clip)
    {
        if (SoundSource == null)
            return;
        SoundSource.clip = clip;
        SoundSource.Play();
    }
}
