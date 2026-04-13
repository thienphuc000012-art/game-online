using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;



public class PlayerManager : NetworkBehaviour
{
    public float moveSpeed = 5f;
    public CharacterController characterController;
    public BasicSpawner spawner;
    public GameObject[] playerModel;

    public TextMeshProUGUI textPlayerName;
    public Image hpFillImage;
    public Image manaFillImage;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        spawner = FindFirstObjectByType<BasicSpawner>();
    }

    public override void Spawned()
    {
        if(Runner.LocalPlayer == Object.InputAuthority)
        {
            var metaData = new PlayerMetaData
            {
                playerName = spawner.LocalPlayerProfile.Name,
                playerClass = spawner.LocalPlayerProfile.Class,
                health = 100,
                mana = 50,
            };

            var playerDataManager = FindFirstObjectByType<PlayerDataManager>();
            playerDataManager.RPC_UpdatePlayerMetaData(Object.InputAuthority, metaData);
        }


        if (Object.HasStateAuthority)
        {
            Debug.Log ("Player Spawned with Authority: " + Object.InputAuthority);
        }
    }
    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority)
        {
           if(GetInput(out NetworkinputData inputData))
            {
                var move = inputData.MoveDirection.normalized;
                if (move.sqrMagnitude > 0)
                {
                    characterController.Move(move * moveSpeed * Runner.DeltaTime);
                }
            }
        }
    }

    public override void Render()
    {
        var playerDataManager = FindFirstObjectByType<PlayerDataManager>();
        if (playerDataManager.TryGetPlayerMetaData(Object.InputAuthority, out var metaData))
        {
            textPlayerName.text = $"{metaData.playerName} - {metaData.playerClass}";

            if (hpFillImage != null)
            {
                hpFillImage.fillAmount = (float)metaData.health / metaData.MaxHealth;
            }

            if (manaFillImage != null)
            {
                manaFillImage.fillAmount = (float)metaData.mana / metaData.MaxMana;
            }
        }

        for (var i = 0; i < playerModel.Length; i++)
        {
            playerModel[i].SetActive(i == (int)metaData.playerClass);
        }
    }
}
