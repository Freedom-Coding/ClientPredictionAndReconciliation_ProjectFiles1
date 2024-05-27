using System.Collections;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using System.Threading.Tasks;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;
using TMPro;
using UnityEngine.UI;
using Unity.Services.Leaderboards.Exceptions;

public class LeaderboardsManager : MonoBehaviour
{
    [HideInInspector] public PlayerControls playerScript;

    [SerializeField] private GameObject leaderboardParent;
    [SerializeField] private Transform leaderboardContentParent;
    [SerializeField] private Transform leaderboardItemPrefab;
    [SerializeField] private Sprite bronzeTierSprite, silverTierSprite, goldenTierSprite;

    private string leaderboardID = "My_Leaderboard";

    private async void Start()
    {
        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        LeaderboardsService.Instance.AddPlayerScoreAsync(leaderboardID, 0);

        leaderboardParent.SetActive(false);
    }

    private async void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (leaderboardParent.activeInHierarchy)
            {
                leaderboardParent.SetActive(false);
            }
            else
            {
                leaderboardParent.SetActive(true);
                UpdateLeaderboard();

                try
                {
                    await LeaderboardsService.Instance.AddPlayerScoreAsync(leaderboardID, playerScript.playerScore.Value);
                }
                catch (LeaderboardsException e)
                {
                    Debug.Log(e.Reason);
                }
                playerScript.playerScore.Value = 0;

            }
        }
    }

    private async void UpdateLeaderboard()
    {
        while (Application.isPlaying && leaderboardParent.activeInHierarchy)
        {
            LeaderboardScoresPage leaderboardScoresPage = await LeaderboardsService.Instance.GetScoresAsync(leaderboardID);

            foreach (Transform t in leaderboardContentParent)
            {
                Destroy(t.gameObject);
            }

            foreach (LeaderboardEntry entry in leaderboardScoresPage.Results)
            {
                Transform leaderboardItem = Instantiate(leaderboardItemPrefab, leaderboardContentParent);
                leaderboardItem.GetChild(0).GetComponent<TextMeshProUGUI>().text = entry.PlayerName;
                leaderboardItem.GetChild(1).GetComponent<TextMeshProUGUI>().text = entry.Score.ToString();

                Sprite tierSprite = null;
                switch (entry.Tier)
                {
                    case "Bronze_tier":
                        tierSprite = bronzeTierSprite;
                        break;
                    case "Silver_tier":
                        tierSprite = silverTierSprite;
                        break;
                    case "Golden_tier":
                        tierSprite = goldenTierSprite;
                        break;
                }

                leaderboardItem.GetChild(2).GetComponent<Image>().sprite = tierSprite;
            }


            await Task.Delay(500);
        }
    }
}