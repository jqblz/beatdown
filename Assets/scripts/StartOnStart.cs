using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StartOnStart : MonoBehaviour
{
    private RhythmController player;

    private void Start()
    {
        player = GetComponent<RhythmController>();
    }

    private void Update()
    {
        if (player.ready && !player.playing)
        {
            player.PlaySong();
        }
    }
}
